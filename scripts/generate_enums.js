const swagger = require("../src/ApiClient/generatedcode/swagger.json");

for (const [name, schema] of Object.entries(swagger.components.schemas)) {
    if (!("enum" in schema)) continue; //skip if true

    console.log(`export enum ${name} {`); //outputs text
    for (let i = 0; i < schema.enum.length; i++) {
        const name = schema["x-enum-varnames"][i]; //sets the name
        const value = schema["enum"][i]; //sets the value
        console.log(`   ${name} = ${value}${i < schema.enum.length - 1 ? "," : ""}`); //outputs to console
    }
    console.log(`}\n`); //creates a newline
}
