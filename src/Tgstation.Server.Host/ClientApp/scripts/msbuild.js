const fs = require("fs-extra");

const destination = "../wwwroot";

//Removes the directory
// noinspection JSUnresolvedFunction //function does exist
fs.removeSync(destination);
//Moves the dist folder to the web root
fs.moveSync("./dist", destination);
