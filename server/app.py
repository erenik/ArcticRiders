#!/usr/bin/env python
# -*- coding: utf-8 -*-

# @Author: mdrhri-6
# @Date:   2016-11-19T10:31:52+01:00
# @Last modified by:   mdrhri-6
# @Last modified time: 2016-11-20T09:09:29+01:00

from flask import Flask, request, json, render_template
from flask_json import FlaskJSON, JsonError, json_response, as_json
import sys

app = Flask(__name__)
app.config['DEBUG'] = True

FlaskJSON(app)

@app.route('/')
def hello():
    return render_template("index.html")

@app.route("/api/getDetails/<int:id>")
@as_json
def get_data(id):
    with open('data.json') as data_file:
        data = json.load(data_file)
    if data.has_key(str(id)):
        result = data[str(id)]
    else:
        result = ""
    return result


if __name__ == '__main__':
    app.run(host='0.0.0.0')
