const { https } = require("follow-redirects");
const fs = require("fs");
const path = require("path");
const pkg = require("../package.json");

const file = fs.createWriteStream(
    path.resolve(__dirname, "..", "src", "ApiClient", "generatedcode", "swagger.json")
);
https.get(
    "https://github.com/tgstation/tgstation-server/releases/download/api-v" +
        pkg.tgs_api_version +
        "/swagger.json",
    function (response) {
        response.pipe(file);
    }
);
