from flask import Flask, render_template, request, make_response, g
import os
import random
import redis
import socket
import logging

option_a = os.getenv('OPTION_A', "Cats")
option_b = os.getenv('OPTION_B', "Dogs")
hostname = socket.gethostname()

app = Flask(__name__)

gunicorn_error_logger = logging.getLogger('gunicorn.error')
app.logger.handlers.extend(gunicorn_error_logger.handlers)
app.logger.setLevel(logging.INFO)

def get_redis():
    if not hasattr(g, 'redis'):
        redis_host = os.getenv('REDIS_HOST', 'redis')
        redis_port = int(os.getenv('REDIS_PORT', 6379))
        app.logger.info(f"Connecting to Redis at {redis_host}:{redis_port}")
        g.redis = redis.Redis(host=redis_host, port=redis_port, db=0, socket_timeout=5)
    return g.redis

@app.route("/", methods=['POST', 'GET'])
def hello():
    voter_id = request.cookies.get('voter_id')
    if not voter_id:
        voter_id = hex(random.getrandbits(64))[2:]

    vote = None

    if request.method == 'POST':
        r = get_redis()
        vote = request.form['vote']
        app.logger.info(f"Voter {voter_id} cast vote for {vote}")
        data = f'{{"voter_id": "{voter_id}", "vote": "{vote}"}}'
        try:
            r.rpush('votes', data)
        except redis.ConnectionError as e:
            app.logger.error(f"Redis connection error: {e}")
            return "Database connection error. Please try again later.", 503

    resp = make_response(render_template(
        'index.html',
        option_a=option_a,
        option_b=option_b,
        hostname=hostname,
        vote=vote,
    ))
    resp.set_cookie('voter_id', voter_id)
    return resp

if __name__ == "__main__":
    app.run(host='0.0.0.0', port=80, debug=True, threaded=True)
