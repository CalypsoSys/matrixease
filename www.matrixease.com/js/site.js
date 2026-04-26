(function (global) {
  function parseTimingValue(value, fallbackSeconds) {
    if (typeof value !== "string" || value.trim() === "") {
      return fallbackSeconds * 1000;
    }

    var normalized = value.trim();
    if (normalized.endsWith("ms")) {
      var milliseconds = Number.parseFloat(normalized.slice(0, -2));
      return Number.isFinite(milliseconds) ? milliseconds : fallbackSeconds * 1000;
    }

    if (normalized.endsWith("s")) {
      normalized = normalized.slice(0, -1);
    }

    var seconds = Number.parseFloat(normalized);
    return Number.isFinite(seconds) ? seconds * 1000 : fallbackSeconds * 1000;
  }

  function buildFeedbackPayload(fields) {
    return {
      Created: "0001-01-01T00:00:00",
      EmailAddress: fields.emailAddress || "",
      Name: fields.name || "",
      Subject: fields.subject || "",
      Message: fields.message || "",
      ClientData: null,
      MessageTypeFkNavigation: null,
    };
  }

  function shouldEnableSticky(scrollY, triggerTop, offset) {
    return scrollY >= Math.max(0, triggerTop - offset);
  }

  function toggleMenu(button, target, expanded) {
    if (!button || !target) {
      return;
    }

    button.setAttribute("aria-expanded", expanded ? "true" : "false");
    target.classList.toggle("show", expanded);
  }

  function initNavbar() {
    var button = global.document.querySelector(".navbar-toggler");
    if (!button) {
      return;
    }

    var targetSelector = button.getAttribute("data-target");
    if (!targetSelector) {
      return;
    }

    var target = global.document.querySelector(targetSelector);
    if (!target) {
      return;
    }

    button.addEventListener("click", function () {
      var expanded = button.getAttribute("aria-expanded") === "true";
      toggleMenu(button, target, !expanded);
    });

    target.querySelectorAll("a").forEach(function (link) {
      link.addEventListener("click", function () {
        if (global.innerWidth < 992) {
          toggleMenu(button, target, false);
        }
      });
    });
  }

  function initStickyHeader() {
    var stickyHost = global.document.querySelector(".addsticky");
    var trigger = global.document.querySelector(".aboutUs");
    if (!stickyHost || !trigger) {
      return;
    }

    function refreshSticky() {
      var triggerTop = trigger.getBoundingClientRect().top + global.scrollY;
      stickyHost.classList.toggle(
        "sticky",
        shouldEnableSticky(global.scrollY, triggerTop, 300)
      );
    }

    refreshSticky();
    global.addEventListener("scroll", refreshSticky, { passive: true });
    global.addEventListener("resize", refreshSticky);
  }

  function initSmoothScroll() {
    global.document.querySelectorAll("a[href^=\"#\"]").forEach(function (link) {
      link.addEventListener("click", function (event) {
        var href = link.getAttribute("href");
        if (!href || href === "#") {
          return;
        }

        var target = global.document.querySelector(href);
        if (!target) {
          return;
        }

        event.preventDefault();
        var targetTop = target.getBoundingClientRect().top + global.scrollY - 85;
        global.scrollTo({
          top: Math.max(0, targetTop),
          behavior: "smooth",
        });
      });
    });
  }

  function initAnimations() {
    var animatedItems = global.document.querySelectorAll(".wow");
    if (!animatedItems.length) {
      return;
    }

    if (!global.IntersectionObserver) {
      animatedItems.forEach(function (item) {
        item.style.visibility = "visible";
        item.style.animationDelay =
          parseTimingValue(item.dataset.wowDelay, 0) + "ms";
        item.style.animationDuration =
          parseTimingValue(item.dataset.wowDuration, 1) + "ms";
        item.classList.add("animated");
      });
      return;
    }

    var observer = new global.IntersectionObserver(
      function (entries, currentObserver) {
        entries.forEach(function (entry) {
          if (!entry.isIntersecting) {
            return;
          }

          var element = entry.target;
          element.style.visibility = "visible";
          element.style.animationDelay =
            parseTimingValue(element.dataset.wowDelay, 0) + "ms";
          element.style.animationDuration =
            parseTimingValue(element.dataset.wowDuration, 1) + "ms";
          element.classList.add("animated");
          currentObserver.unobserve(element);
        });
      },
      {
        threshold: 0.15,
      }
    );

    animatedItems.forEach(function (item) {
      item.style.visibility = "hidden";
      observer.observe(item);
    });
  }

  function initContactForm() {
    var form = global.document.getElementById("learn_more_id");
    if (!form) {
      return;
    }

    form.addEventListener("submit", function (event) {
      event.preventDefault();

      var payload = buildFeedbackPayload({
        emailAddress: global.document.getElementById("EMAIL_ADDRESS_ID").value,
        name: global.document.getElementById("NAME_ID").value,
        subject: global.document.getElementById("SUBJECT_ID").value,
        message: global.document.getElementById("MESSAGE_ID").value,
      });

      global
        .fetch("https://my.matrixease.com/api/feedback/save_message/", {
          method: "POST",
          headers: {
            "Content-Type": "application/json;charset=UTF-8",
          },
          body: JSON.stringify(payload),
        })
        .then(function (response) {
          return response
            .json()
            .catch(function () {
              return { message: response.ok ? "Message sent." : "Unable to send message." };
            })
            .then(function (data) {
              if (!response.ok) {
                throw new Error(data.message || "Unable to send message.");
              }

              global.alert(data.message || "Message sent.");
              form.reset();
            });
        })
        .catch(function (error) {
          global.alert(error.message || "Unable to send message.");
        });
    });
  }

  function setModalState(modal, open) {
    if (!modal) {
      return;
    }

    modal.classList.toggle("show", open);
    modal.style.display = open ? "block" : "none";
    modal.setAttribute("aria-hidden", open ? "false" : "true");
    global.document.body.classList.toggle("modal-open", open);

    var existingBackdrop = global.document.querySelector(".modal-backdrop");
    if (open && !existingBackdrop) {
      var backdrop = global.document.createElement("div");
      backdrop.className = "modal-backdrop fade show";
      backdrop.dataset.generated = "true";
      backdrop.addEventListener("click", function () {
        setModalState(modal, false);
      });
      global.document.body.appendChild(backdrop);
    }

    if (!open && existingBackdrop && existingBackdrop.dataset.generated === "true") {
      existingBackdrop.remove();
    }
  }

  function initModals() {
    var triggers = global.document.querySelectorAll("[data-toggle=\"modal\"]");
    if (!triggers.length) {
      return;
    }

    triggers.forEach(function (trigger) {
      var targetSelector = trigger.getAttribute("data-target");
      var modal = targetSelector
        ? global.document.querySelector(targetSelector)
        : null;
      if (!modal) {
        return;
      }

      trigger.addEventListener("click", function (event) {
        event.preventDefault();
        setModalState(modal, true);
      });
    });

    global.document.querySelectorAll("[data-dismiss=\"modal\"]").forEach(function (button) {
      button.addEventListener("click", function () {
        var modal = button.closest(".modal");
        setModalState(modal, false);
      });
    });

    global.document.querySelectorAll(".modal").forEach(function (modal) {
      modal.addEventListener("click", function (event) {
        if (event.target === modal) {
          setModalState(modal, false);
        }
      });
    });

    global.document.addEventListener("keydown", function (event) {
      if (event.key !== "Escape") {
        return;
      }

      var openModal = global.document.querySelector(".modal.show");
      if (openModal) {
        setModalState(openModal, false);
      }
    });
  }

  function init() {
    if (!global.document) {
      return;
    }

    initNavbar();
    initStickyHeader();
    initSmoothScroll();
    initAnimations();
    initContactForm();
    initModals();
  }

  if (typeof module !== "undefined" && module.exports) {
    module.exports = {
      buildFeedbackPayload: buildFeedbackPayload,
      parseTimingValue: parseTimingValue,
      shouldEnableSticky: shouldEnableSticky,
    };
  }

  if (global.document) {
    global.document.addEventListener("DOMContentLoaded", init);
  }
})(typeof window !== "undefined" ? window : globalThis);
