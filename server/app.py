# @Author: mdrhri-6
# @Date:   2016-11-19T10:31:52+01:00
# @Last modified by:   mdrhri-6
# @Last modified time: 2016-11-19T13:37:45+01:00


from __future__ import print_function
from flask import Flask
from flask import json
import sys

app = Flask(__name__)

@app.route("/api/getDetails/<int:id>")
def hello(id):
    # with open('data.json') as data_file:
    #     data = json.loads(data_file)
    # print (data, file = sys.stderr)
    data = json.loads(open("data.json"))
    return data


if __name__ == '__main__':
    app.run()
