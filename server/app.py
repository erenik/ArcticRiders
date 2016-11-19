# @Author: mdrhri-6
# @Date:   2016-11-19T10:31:52+01:00
# @Last modified by:   mdrhri-6
# @Last modified time: 2016-11-19T16:08:46+01:00

# from __future__ import unicode_literals
# import unicodedata
from flask import Flask, request, json
from flask_json import FlaskJSON, JsonError, json_response, as_json
import sys

app = Flask(__name__)
app.config['DEBUG'] = True

FlaskJSON(app)

@app.route("/api/getDetails/<int:id>")
@as_json
def hello(id):
    with open('data.json') as data_file:
        data = json.load(data_file)
    if data.has_key(str(id)):
        result = data[str(id)]
    else:
        result = ""
    return result


if __name__ == '__main__':
    app.run(host='0.0.0.0')
