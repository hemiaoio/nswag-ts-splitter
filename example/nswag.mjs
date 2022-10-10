import { exec } from 'child_process';
import fs from 'fs';
import path from 'path';
import { createInterface } from 'readline';
function askQuestion(query) {
    const rl = createInterface({
        input: process.stdin,
        output: process.stdout,
    });

    return new Promise(resolve => rl.question(query, ans => {
        rl.close();
        resolve(ans);
    }))
}
const cwd = path.dirname(process.argv[1]);
let targetService = "";
if (process.argv.length < 3) {
    targetService = await askQuestion('Please input the target service name > ')
} else {
    targetService = process.argv[2];
}
const configPath = path.join(cwd, 'nswag', `service.config${!!targetService ? "." + targetService : ""}.nswag`);
const configFileAccess = fs.existsSync(configPath);
if (!configFileAccess) {
    const errorMessage = "Target service [" + targetService + "] config dose not exists, \n Please confirm path [" + configPath + "] is exist.";
    throw new Error(errorMessage);
}

let execPath = "";
if (process.platform.startsWith("win")) {
    execPath = path.join(cwd, 'nswag', 'win-x64', 'NSwagTsSplitter.exe');
} else if (process.platform.startsWith("linux")) {
    execPath = path.join(cwd, 'nswag', 'linux-x64', 'NSwagTsSplitter');
    fs.access(execPath, fs.constants.X_OK, function (error) {
        if (error) {
            exec('sudo chmod', ['+', 'x', execPath], function (err, stdout, stderr) {
                if (err) {
                    console.error(err);
                }
            })
        }
    });
} else if (process.platform.startsWith("mac")) {
    execPath = path.join(cwd, 'nswag', 'osx-x64', 'NSwagTsSplitter');
    fs.access(execPath, fs.constants.X_OK, function (error) {
        if (error) {
            exec('sudo chmod', ['+', 'x', execPath], function (err, stdout, stderr) {
                if (err) {
                    console.error(err);
                }
            })
        }
    });
}
exec(`${execPath} -c ${configPath}`,
    function (err, stdout, stderr) {
        if (err) {
            console.error(err);
        }
        console.log(stdout)
    })