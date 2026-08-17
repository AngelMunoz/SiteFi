// SiteGen — hand-written vanilla JS. No build step, no dependencies.
// 1. Mobile drawer toggle
// 2. Dark/light theme toggle (persisted in localStorage)
// 3. highlight.js init
(function () {
  "use strict";

  function toggleDrawer() {
    var backdrop = document.querySelector(".drawer-backdrop");
    var drawer = document.getElementById("drawer");
    if (backdrop) backdrop.classList.toggle("shown");
    if (drawer) drawer.classList.toggle("shown");
  }

  document.querySelectorAll("[data-drawer-toggle]").forEach(function (el) {
    el.addEventListener("click", toggleDrawer);
  });
  document.querySelectorAll("[data-drawer-close]").forEach(function (el) {
    el.addEventListener("click", toggleDrawer);
  });

  var themeBtn = document.getElementById("theme-toggle");
  if (themeBtn) {
    themeBtn.addEventListener("click", function () {
      var root = document.documentElement;
      var current =
        root.dataset.theme ||
        (window.matchMedia("(prefers-color-scheme: dark)").matches
          ? "dark"
          : "light");
      var next = current === "dark" ? "light" : "dark";
      root.dataset.theme = next;
      try {
        localStorage.setItem("theme", next);
      } catch (e) {}
    });
  }

  if (window.hljs) {
    hljs.highlightAll();
  }
})();
