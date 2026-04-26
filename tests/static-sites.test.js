const test = require("node:test");
const assert = require("node:assert/strict");

const wwwSite = require("../www.matrixease.com/js/site.js");
const docsSite = require("../docs.matrixease.com/assets/js/site.js");

test("buildFeedbackPayload produces the legacy API shape", function () {
  const payload = wwwSite.buildFeedbackPayload({
    emailAddress: "test@example.com",
    name: "Test User",
    subject: "Hello",
    message: "World",
  });

  assert.deepEqual(payload, {
    Created: "0001-01-01T00:00:00",
    EmailAddress: "test@example.com",
    Name: "Test User",
    Subject: "Hello",
    Message: "World",
    ClientData: null,
    MessageTypeFkNavigation: null,
  });
});

test("parseTimingValue supports seconds and milliseconds", function () {
  assert.equal(wwwSite.parseTimingValue(".5s", 1), 500);
  assert.equal(wwwSite.parseTimingValue("250ms", 1), 250);
  assert.equal(wwwSite.parseTimingValue("", 2), 2000);
});

test("shouldEnableSticky turns on once the threshold is crossed", function () {
  assert.equal(wwwSite.shouldEnableSticky(250, 600, 300), false);
  assert.equal(wwwSite.shouldEnableSticky(300, 600, 300), true);
});

test("shouldAnimateEntry only animates when the observed item intersects", function () {
  assert.equal(wwwSite.shouldAnimateEntry({ isIntersecting: false }), false);
  assert.equal(wwwSite.shouldAnimateEntry({ isIntersecting: true }), true);
});

test("applyRevealState shows the element and applies reveal timing", function () {
  const classes = new Set();
  const element = {
    dataset: {
      revealDelay: ".2s",
      revealDuration: "900ms",
    },
    style: {},
    classList: {
      add(value) {
        classes.add(value);
      },
    },
  };

  wwwSite.applyRevealState(element);

  assert.equal(element.style.visibility, "visible");
  assert.equal(element.style.transitionDelay, "200ms");
  assert.equal(element.style.transitionDuration, "900ms");
  assert.equal(classes.has("is-visible"), true);
});

test("applyLoadRevealState shows the element and applies load timing", function () {
  const classes = new Set();
  const element = {
    dataset: {
      loadDelay: ".3s",
      loadDuration: "1.2s",
    },
    style: {},
    classList: {
      add(value) {
        classes.add(value);
      },
    },
  };

  wwwSite.applyLoadRevealState(element);

  assert.equal(element.style.visibility, "visible");
  assert.equal(element.style.transitionDelay, "300ms");
  assert.equal(element.style.transitionDuration, "1200ms");
  assert.equal(classes.has("is-visible"), true);
});

test("getMaxHeight returns the tallest measured block", function () {
  assert.equal(docsSite.getMaxHeight([120, 320, 280]), 320);
});

test("findActiveSectionId returns the last section above the viewport offset", function () {
  const activeId = docsSite.findActiveSectionId(
    [
      { id: "intro", top: 100 },
      { id: "details", top: 500 },
      { id: "faq", top: 1000 },
    ],
    420,
    120
  );

  assert.equal(activeId, "details");
});

test("syncActiveNavState marks the active item and its parent section", function () {
  function createListItem(href, parentListItem) {
    const classes = new Set();
    const listItem = {
      classList: {
        add(value) {
          classes.add(value);
        },
        toggle(value, enabled) {
          if (enabled) {
            classes.add(value);
            return;
          }

          classes.delete(value);
        },
        contains(value) {
          return classes.has(value);
        },
      },
      parentElement: {
        closest(selector) {
          if (selector === "li") {
            return parentListItem || null;
          }

          return null;
        },
      },
    };

    return {
      link: {
        getAttribute(name) {
          return name === "href" ? href : null;
        },
        parentElement: listItem,
      },
      listItem: listItem,
    };
  }

  const parentClasses = new Set();
  const parentListItem = {
    classList: {
      add(value) {
        parentClasses.add(value);
      },
      contains(value) {
        return parentClasses.has(value);
      },
    },
  };

  const intro = createListItem("#intro", null);
  const details = createListItem("#details", parentListItem);

  docsSite.syncActiveNavState([intro.link, details.link], "details");

  assert.equal(intro.listItem.classList.contains("active"), false);
  assert.equal(details.listItem.classList.contains("active"), true);
  assert.equal(parentListItem.classList.contains("active"), true);
});
