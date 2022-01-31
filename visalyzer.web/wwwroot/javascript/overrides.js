function customWindowOpen(url, target, data) {
    var epoch = Date.now();
    var win = window.open(url + "?id=" + epoch, target);
    win[epoch] = JSON.stringify(data);
}

function renderWindowData(id, callback) {
    callback(JSON.parse(window[id]));
}

function openContactUs() {
    window.open("https://visalyzer.com#contact-us", "_blank");
}

function openAbout() {
    window.open("https://www.visalyzer.com/index.html#about", "_blank");
}

function openDocs() {
    window.open("https://docs.visalyzer.com", "_blank");
}

function openBlog() {
    window.open("https://blog.visalyzer.com", "_blank");
}

function openSupport() {
    window.open("https://support.visalyzer.com", "_blank");
}
