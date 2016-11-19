# @Author: mdrhri-6
# @Date:   2016-11-19T10:31:52+01:00
# @Last modified by:   mdrhri-6
# @Last modified time: 2016-11-19T11:03:24+01:00



from flask import Flask

app = Flask(__name__)

@app.route("/api/getDetails/<int:id>")
def hello(id):
    return "Hello Worlds!!" + str(id)';'


if __name__ == '__main__':
    app.run()
