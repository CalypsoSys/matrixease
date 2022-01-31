function customWindowOpen(url, target, data) {
    var epoch = Date.now();
    var x = require('electron').ipcRenderer.send("open_window", ["http://localhost:51266" + url + "?id=" + epoch, epoch, data])
    /*
    const BrowserWindow = require('electron').remote.BrowserWindow;
    const win = new BrowserWindow({
        webPreferences: {
            nodeIntegration: true,
            enableRemoteModule: true,
            //autoHideMenuBar: true
        }});

    win.loadURL("http://localhost:51266" + url);
    */
}

function renderWindowData(id, callback) {
    var ipcRenderer = require('electron').ipcRenderer;
    ipcRenderer.on('send_data', function (event, store) {
        callback(store);
    });

    ipcRenderer.send("opened_window", id);
}

function openContactUs() {
    require('electron').shell.openExternal("https://visalyzer.com#contact-us");
}

function openAbout() {
    require('electron').shell.openExternal("https://www.visalyzer.com/index.html#about");
}

function openDocs() {
    require('electron').shell.openExternal("https://docs.visalyzer.com");
}

function openBlog() {
    require('electron').shell.openExternal("https://blog.visalyzer.com");
}

function openSupport() {
    require('electron').shell.openExternal("https://support.visalyzer.com");
}
