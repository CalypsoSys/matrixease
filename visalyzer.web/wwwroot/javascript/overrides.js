function customWindowOpen(url, target, data) {
    var epoch = Date.now();
    var win = window.open(url + "?id=" + epoch, target);
    win[epoch] = JSON.stringify(data);
}

function renderWindowData(id, callback) {
    callback(JSON.parse(window[id]));
}

function openContactUs() {
    window.open("https://www.visalyzer.com#contact-us", "visalyzer_contact");
}

function openAbout() {
    window.open("https://www.visalyzer.com/index.html#about", "visalyzer_acount");
}

function openDocs(spec) {
    if (spec) {
        window.open("https://docs.visalyzer.com" + spec, "visalyzer_docs_popup", "toolbar=yes,scrollbars=yes,resizable=yes,width=400,height=400");
    } else {
        window.open("https://docs.visalyzer.com", "visalyzer_docs");
    }
}

function openBlog() {
    window.open("https://blog.visalyzer.com", "visalyzer_blog");
}

function openSupport() {
    window.open("https://support.visalyzer.com", "visalyzer_support");
}
