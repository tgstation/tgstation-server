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

const svg_bytes = fs.readFileSync("../../build/logo.svg");
const svg = svg_bytes.toString();
const white_bg_svg = svg
    .replace("<!-- DO NOT CHANGE THIS LINE, UNCOMMENTING IT ENABLES THE WHITE BACKGROUND FOR THE .ICO--><!--", "")
    .replace("SCRIPT_REPLACE_TOKEN", "");
fs.writeFileSync("logo_white_bg.svg", white_bg_svg);

svg_to_img({
   input_name: "logo_white_bg.svg",
   output_name: "../../artifacts/tgs.ico",
   sizes: [ 160 ]
}).then(() => {
    fs.unlinkSync("logo_white_bg.svg");
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
    fs.unlinkSync("logo_white_bg.svg");
    console.error(`ICO conversion failed: ${error}`);
    exit(1);
});
