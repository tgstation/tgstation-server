// Prereq packages: svg-to-ico@1.0.14 svg2img@1.0.0-beta.2
// Usage: node ./build_logo.js
// Generates ../../artifacts/tgs.ico and ../../artifacts/tgs.ico 

const svg_to_img = require("svg-to-ico");
const svg2img = require('svg2img');
const fs = require('fs');
const { exit } = require("process");
if (!fs.existsSync("../../artifacts")) {
    fs.mkdirSync("../../artifacts",'0777', true);
}

svg_to_img({
   input_name: "../../build/logo.svg",
   output_name: "../../artifacts/tgs.ico",
   sizes: [ 160 ]
}).then(() => {
    // this package sucks, path separators are fucked
    svg2img(
        "../../build/logo.svg",
        {
            resvg: {
                fitTo: {
                    mode: 'width', // or height
                    value: 64,
                }
            }
        },
        function(error, buffer) {
            if(error) {
                console.error(`PNG conversion failed: ${error}`);
                exit(2);
            }

            fs.writeFileSync("../../artifacts/tgs.png", buffer);
        });
}).catch((error) => {
   console.error(`ICO conversion failed: ${error}`);
   exit(1);
});
