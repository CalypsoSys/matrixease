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