function customWindowOpen(url, target, data) {
    var epoch = Date.now();
    var win = window.open(url + "?id=" + epoch, target);
    win[epoch] = JSON.stringify(data);
}

function renderWindowData(id, callback) {
    callback(JSON.parse(window[id]));
}

function openContactUs() {
    window.open("https://www.matrixease.com#contact-us", "matrixease_contact");
}

function openAbout() {
    window.open("https://www.matrixease.com/index.html#about", "matrixease_acount");
}

function openDocs(spec) {
    if (spec) {
        window.open("https://docs.matrixease.com" + spec, "matrixease_docs_popup", "toolbar=yes,scrollbars=yes,resizable=yes,width=400,height=400");
    } else {
        window.open("https://docs.matrixease.com", "matrixease_docs");
    }
}

function openBlog() {
    window.open("https://blog.matrixease.com", "matrixease_blog");
}

function openSupport() {
    window.open("https://support.matrixease.com", "matrixease_support");
}
