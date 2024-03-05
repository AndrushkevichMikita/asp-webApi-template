/** Basic functions ***************************************************************************/
const { spawn } = require("child_process");
const fs = require("fs");
const path = require("path");
const os = require("os");
const { chdir } = require("process");

function exec(cfg, cmd, { args, isGetResult, silent } = {}) {
  return new Promise((resolve, reject) => {
    // !silent && cfg.env.log("exec", cmd);
    const child = spawn(cmd, args || ["--progress"], {
      shell: true,
      stdio: isGetResult ? "pipe" : "inherit",
      // cwd: require("process").cwd(),
    });
    let msg;
    child.stdout?.on("data", (data) => {
      msg = data.toString();
    });
    child.stderr?.on("data", (data) => {
      msg = data.toString();
    });
    child.on("error", reject);
    child.on("exit", (code) =>
      code === 0 ? resolve(msg) : reject({ code, msg })
    );
  }).catch((err) => {
    // cfg.env.logError(`exec of\n'${cmd}'\nwas rejected with code: ${err.code}`);
    throw err;
  });
}

(async () => {
  await exec({}, `docker-compose up -d`, { args: [] });
  await exec({}, `dotnet test Tests`, { args: [] });
})();
