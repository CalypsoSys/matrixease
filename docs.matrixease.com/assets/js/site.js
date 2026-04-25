(function (global) {
  function getMaxHeight(values) {
    return values.reduce(function (max, value) {
      return Math.max(max, value);
    }, 0);
  }

  function findActiveSectionId(sectionOffsets, scrollPosition, offset) {
    var activeId = "";

    sectionOffsets.forEach(function (section) {
      if (scrollPosition + offset >= section.top) {
        activeId = section.id;
      }
    });

    return activeId;
  }

  function syncActiveNavState(links, activeId) {
    links.forEach(function (link) {
      var href = link.getAttribute("href");
      var listItem = link.parentElement;
      if (!listItem) {
        return;
      }

      var isActive = Boolean(activeId) && href === "#" + activeId;
      listItem.classList.toggle("active", isActive);

      if (isActive) {
        var parentItem = listItem.parentElement && listItem.parentElement.closest("li");
        if (parentItem) {
          parentItem.classList.add("active");
        }
      }
    });
  }

  function equalizeHeights(selector) {
    var elements = Array.from(global.document.querySelectorAll(selector));
    if (!elements.length) {
      return;
    }

    elements.forEach(function (element) {
      element.style.minHeight = "";
    });

    var maxHeight = getMaxHeight(
      elements.map(function (element) {
        return element.getBoundingClientRect().height;
      })
    );

    elements.forEach(function (element) {
      element.style.minHeight = maxHeight + "px";
    });
  }

  function initEqualHeights() {
    equalizeHeights("#cards-wrapper .item-inner");
    equalizeHeights("#showcase .card");
    global.addEventListener("resize", function () {
      equalizeHeights("#cards-wrapper .item-inner");
      equalizeHeights("#showcase .card");
    });
  }

  function initSmoothScroll() {
    global.document.querySelectorAll("a.scrollto").forEach(function (link) {
      link.addEventListener("click", function (event) {
        var href = link.getAttribute("href");
        if (!href || !href.startsWith("#")) {
          return;
        }

        var target = global.document.querySelector(href);
        if (!target) {
          return;
        }

        event.preventDefault();
        var top = target.getBoundingClientRect().top + global.scrollY;
        global.scrollTo({
          top: Math.max(0, top),
          behavior: "smooth",
        });
      });
    });
  }

  function initScrollSpy() {
    var nav = global.document.getElementById("doc-nav");
    if (!nav) {
      return;
    }

    var links = Array.from(nav.querySelectorAll("a.scrollto"));
    var sections = links
      .map(function (link) {
        var href = link.getAttribute("href");
        if (!href || !href.startsWith("#")) {
          return null;
        }

        var section = global.document.querySelector(href);
        return section ? { id: href.slice(1), element: section, link: link } : null;
      })
      .filter(Boolean);

    if (!sections.length) {
      return;
    }

    function refreshActiveLink() {
      var offsets = sections.map(function (section) {
        return {
          id: section.id,
          top: section.element.getBoundingClientRect().top + global.scrollY,
        };
      });

      var activeId = findActiveSectionId(offsets, global.scrollY, 120);
      syncActiveNavState(links, activeId);
    }

    refreshActiveLink();
    global.addEventListener("scroll", refreshActiveLink, { passive: true });
    global.addEventListener("resize", refreshActiveLink);
  }

  function closeLightbox(lightbox) {
    if (!lightbox) {
      return;
    }

    lightbox.classList.remove("is-visible");
    global.document.body.classList.remove("site-lightbox-open");
  }

  function initLightbox() {
    var lightboxLinks = Array.from(
      global.document.querySelectorAll("[data-toggle=\"lightbox\"]")
    );
    if (!lightboxLinks.length) {
      return;
    }

    var lightbox = global.document.createElement("div");
    lightbox.className = "site-lightbox";
    lightbox.innerHTML =
      "<button type=\"button\" class=\"site-lightbox__close\" aria-label=\"Close\">&times;</button>" +
      "<figure class=\"site-lightbox__figure\">" +
      "<img class=\"site-lightbox__image\" alt=\"\" />" +
      "<figcaption class=\"site-lightbox__caption\"></figcaption>" +
      "</figure>";

    var image = lightbox.querySelector(".site-lightbox__image");
    var caption = lightbox.querySelector(".site-lightbox__caption");
    var closeButton = lightbox.querySelector(".site-lightbox__close");

    global.document.body.appendChild(lightbox);

    lightboxLinks.forEach(function (link) {
      link.addEventListener("click", function (event) {
        event.preventDefault();
        image.src = link.getAttribute("href") || "";
        image.alt = link.getAttribute("data-title") || "";
        caption.textContent = link.getAttribute("data-title") || "";
        lightbox.classList.add("is-visible");
        global.document.body.classList.add("site-lightbox-open");
      });
    });

    closeButton.addEventListener("click", function () {
      closeLightbox(lightbox);
    });

    lightbox.addEventListener("click", function (event) {
      if (event.target === lightbox) {
        closeLightbox(lightbox);
      }
    });

    global.document.addEventListener("keydown", function (event) {
      if (event.key === "Escape") {
        closeLightbox(lightbox);
      }
    });
  }

  function init() {
    if (!global.document) {
      return;
    }

    initEqualHeights();
    initSmoothScroll();
    initScrollSpy();
    initLightbox();
  }

  if (typeof module !== "undefined" && module.exports) {
    module.exports = {
      findActiveSectionId: findActiveSectionId,
      getMaxHeight: getMaxHeight,
      syncActiveNavState: syncActiveNavState,
    };
  }

  if (global.document) {
    global.document.addEventListener("DOMContentLoaded", init);
  }
})(typeof window !== "undefined" ? window : globalThis);
