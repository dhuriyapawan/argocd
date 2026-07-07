const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const { Pool } = require('pg');
const path = require('path');

const app = express();
const server = http.createServer(app);
const io = new Server(server);

const port = process.env.PORT || 80;

// Setup PostgreSQL client connection pool
const pgHost = process.env.PG_HOST || 'db';
const pgPort = process.env.PG_PORT || 5432;
const pgUser = process.env.PG_USER || 'postgres';
const pgPassword = process.env.PG_PASSWORD || 'postgres';
const pgDatabase = process.env.PG_DATABASE || 'postgres';

const pool = new Pool({
  host: pgHost,
  port: pgPort,
  user: pgUser,
  password: pgPassword,
  database: pgDatabase,
  max: 10,
  idleTimeoutMillis: 30000,
  connectionTimeoutMillis: 5000,
});

// Retry Postgres connection until successful
function checkPgConnection() {
  pool.query('SELECT NOW()', (err, res) => {
    if (err) {
      console.error('Error connecting to PostgreSQL, retrying in 2 seconds...', err.message);
      setTimeout(checkPgConnection, 2000);
    } else {
      console.log('Successfully connected to PostgreSQL at ' + pgHost + ':' + pgPort);
      startPolling();
    }
  });
}

checkPgConnection();

// Serves the static index.html from views folder
app.use(express.static(path.join(__dirname, 'views')));

app.get('/', (req, res) => {
  res.sendFile(path.join(__dirname, 'views', 'index.html'));
});

// Poll the database for vote counts and emit via socket.io
function startPolling() {
  setInterval(async () => {
    try {
      const queryResult = await pool.query('SELECT vote, COUNT(id) AS count FROM votes GROUP BY vote');
      const votes = { a: 0, b: 0 };
      
      queryResult.rows.forEach((row) => {
        if (row.vote === 'a') votes.a = parseInt(row.count, 10);
        if (row.vote === 'b') votes.b = parseInt(row.count, 10);
      });
      
      io.emit('scores', votes);
    } catch (err) {
      console.error('Error querying votes table: ', err.message);
    }
  }, 1000);
}

// Socket.io connection logging
io.on('connection', (socket) => {
  console.log(`New client connected: ${socket.id}`);
  
  // Send initial 0-0 score or empty score right away
  socket.emit('scores', { a: 0, b: 0 });

  socket.on('disconnect', () => {
    console.log(`Client disconnected: ${socket.id}`);
  });
});

server.listen(port, () => {
  console.log(`Result service listening on port ${port}`);
});
