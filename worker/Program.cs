using System;
using System.Data;
using System.Text.Json;
using System.Threading;
using Npgsql;
using StackExchange.Redis;

namespace Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Worker service starting up...");

            // Get Environment Variables or fallbacks
            var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "redis";
            var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
            var redisConnectionString = $"{redisHost}:{redisPort},abortConnect=false,connectRetry=5,connectTimeout=5000";

            var pgHost = Environment.GetEnvironmentVariable("PG_HOST") ?? "db";
            var pgPort = Environment.GetEnvironmentVariable("PG_PORT") ?? "5432";
            var pgUser = Environment.GetEnvironmentVariable("PG_USER") ?? "postgres";
            var pgPassword = Environment.GetEnvironmentVariable("PG_PASSWORD") ?? "postgres";
            var pgDatabase = Environment.GetEnvironmentVariable("PG_DATABASE") ?? "postgres";
            var pgConnectionString = $"Host={pgHost};Port={pgPort};Username={pgUser};Password={pgPassword};Database={pgDatabase};Timeout=15;";

            // Connect to Postgres
            NpgsqlConnection? pgConn = null;
            while (pgConn == null)
            {
                try
                {
                    Console.WriteLine($"Connecting to PostgreSQL database at {pgHost}:{pgPort}...");
                    pgConn = new NpgsqlConnection(pgConnectionString);
                    pgConn.Open();
                    Console.WriteLine("Successfully connected to PostgreSQL database.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to connect to PostgreSQL. Retrying in 2 seconds... Error: {ex.Message}");
                    pgConn = null;
                    Thread.Sleep(2000);
                }
            }

            // Ensure schema exists
            try
            {
                using var cmd = new NpgsqlCommand(
                    "CREATE TABLE IF NOT EXISTS votes (id VARCHAR(255) PRIMARY KEY, vote VARCHAR(255) NOT NULL)", 
                    pgConn
                );
                cmd.ExecuteNonQuery();
                Console.WriteLine("Database schema verified (table 'votes' exists).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying database schema: {ex.Message}");
                Environment.Exit(1);
            }

            // Connect to Redis
            ConnectionMultiplexer? redis = null;
            IDatabase? redisDb = null;
            while (redis == null)
            {
                try
                {
                    Console.WriteLine($"Connecting to Redis at {redisHost}:{redisPort}...");
                    redis = ConnectionMultiplexer.Connect(redisConnectionString);
                    redisDb = redis.GetDatabase();
                    Console.WriteLine("Successfully connected to Redis.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to connect to Redis. Retrying in 2 seconds... Error: {ex.Message}");
                    redis = null;
                    Thread.Sleep(2000);
                }
            }

            Console.WriteLine("Worker is ready. Watching for votes...");

            // Main processing loop
            while (true)
            {
                try
                {
                    // Pop from the left of the 'votes' list
                    var rawVote = redisDb.ListLeftPop("votes");

                    if (!rawVote.IsNull)
                    {
                        var voteStr = rawVote.ToString();
                        Console.WriteLine($"Processing vote data: {voteStr}");

                        using var doc = JsonDocument.Parse(voteStr);
                        var root = doc.RootElement;
                        var voterId = root.GetProperty("voter_id").GetString();
                        var voteValue = root.GetProperty("vote").GetString();

                        if (!string.IsNullOrEmpty(voterId) && !string.IsNullOrEmpty(voteValue))
                        {
                            UpdateVote(pgConn, voterId, voteValue);
                        }
                    }
                    else
                    {
                        // Sleep a bit if no items in Redis queue to prevent high CPU utilization
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing vote: {ex.Message}");
                    
                    // If Postgres connection is closed/broken, try to reconnect
                    if (pgConn.State != ConnectionState.Open)
                    {
                        Console.WriteLine("PostgreSQL connection lost. Attempting to reconnect...");
                        try
                        {
                            pgConn.Close();
                            pgConn.Open();
                            Console.WriteLine("Reconnected to PostgreSQL.");
                        }
                        catch
                        {
                            Thread.Sleep(2000);
                        }
                    }
                }
            }
        }

        private static void UpdateVote(NpgsqlConnection conn, string voterId, string vote)
        {
            const string sql = "INSERT INTO votes (id, vote) VALUES (@id, @vote) ON CONFLICT (id) DO UPDATE SET vote = EXCLUDED.vote";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", voterId);
            cmd.Parameters.AddWithValue("@vote", vote);
            cmd.ExecuteNonQuery();
            Console.WriteLine($"Persisted vote for voter '{voterId}': '{vote}'");
        }
    }
}
