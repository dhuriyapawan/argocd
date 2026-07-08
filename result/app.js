const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const { Pool } = require('pg');
const path = require('path');

const app = express();
const server = http.createServer(app);
const io = new Server(server);

const PORT = process.env.PORT || 80;

// PostgreSQL Configuration
const pool = new Pool({
  host: process.env.PG_HOST || 'voting-app-postgres',
  port: process.env.PG_PORT || 5432,
  user: process.env.PG_USER || 'postgres',
  password: process.env.PG_PASSWORD || 'postgres',
  database: process.env.PG_DATABASE || 'votingdb',
  max: 10,
  idleTimeoutMillis: 30000,
  connectionTimeoutMillis: 5000,
});

app.use(express.static(path.join(__dirname, 'views')));

app.get('/', (req, res) => {
  res.sendFile(path.join(__dirname, 'views', 'index.html'));
});

// Get vote counts from PostgreSQL
async function getVoteCounts() {
  const votes = {
    a: 0,
    b: 0,
  };

  try {
    const result = await pool.query(
      `SELECT vote, COUNT(*) AS count
       FROM votes
       GROUP BY vote`
    );

    result.rows.forEach((row) => {
      votes[row.vote] = parseInt(row.count, 10);
    });

    return votes;
  } catch (err) {
    console.error("Database query failed:", err.message);
    return votes;
  }
}

// Check PostgreSQL Connection
async function checkDatabase() {
  try {
    await pool.query("SELECT NOW()");
    console.log(
      `Successfully connected to PostgreSQL at ${process.env.PG_HOST || 'voting-app-postgres'}:${process.env.PG_PORT || 5432}`
    );

    startPolling();

  } catch (err) {
    console.error("Unable to connect to PostgreSQL:", err.message);
    setTimeout(checkDatabase, 2000);
  }
}

let pollingStarted = false;

function startPolling() {
  if (pollingStarted) return;

  pollingStarted = true;

  setInterval(async () => {
    const votes = await getVoteCounts();

    console.log("Current Votes:", votes);

    io.emit("scores", votes);

  }, 1000);
}

// Socket.IO
io.on("connection", async (socket) => {

  console.log(`Client Connected: ${socket.id}`);

  // Send current database values immediately
  const votes = await getVoteCounts();

  socket.emit("scores", votes);

  socket.on("disconnect", () => {
    console.log(`Client Disconnected: ${socket.id}`);
  });

});

checkDatabase();

server.listen(PORT, () => {
  console.log(`Result service listening on port ${PORT}`);
});