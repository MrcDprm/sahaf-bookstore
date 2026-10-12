// Small progressive enhancements; every page also works without JavaScript.
document.addEventListener("DOMContentLoaded", function () {
  // Theme toggle
  var toggle = document.querySelector("[data-theme-toggle]");
  if (toggle) {
    toggle.addEventListener("click", function () {
      var next = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
      document.documentElement.dataset.theme = next;
      try { localStorage.setItem("sahaf-theme", next); } catch (e) { /* storage blocked */ }
    });
  }

  // Spine and book colours come from data attributes: the CSP blocks inline styles.
  document.querySelectorAll("[data-bg]").forEach(function (el) {
    el.style.setProperty("--bg", el.dataset.bg);
    if (el.dataset.fg) el.style.setProperty("--fg", el.dataset.fg);
    if (el.dataset.accent) el.style.setProperty("--accent", el.dataset.accent);
  });

  // Submit the sort form as soon as the selection changes.
  document.querySelectorAll("form[data-autosubmit] select").forEach(function (select) {
    select.addEventListener("change", function () { select.form.submit(); });
  });

  // Quantity stepper
  document.querySelectorAll(".stepper").forEach(function (stepper) {
    var input = stepper.querySelector("input");
    stepper.querySelectorAll("[data-step]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var min = Number(input.min) || 1, max = Number(input.max) || 1000;
        var value = (parseInt(input.value, 10) || min) + Number(btn.dataset.step);
        input.value = Math.min(max, Math.max(min, value));
      });
    });
  });

  // Ask before destructive actions (delete, cancel, lock).
  document.querySelectorAll("form[data-confirm]").forEach(function (form) {
    form.addEventListener("submit", function (e) {
      if (!window.confirm(form.dataset.confirm)) e.preventDefault();
    });
  });

  // Close open dropdowns when clicking elsewhere.
  document.addEventListener("click", function (e) {
    document.querySelectorAll("details.menu[open]").forEach(function (menu) {
      if (!menu.contains(e.target)) menu.removeAttribute("open");
    });
  });

  // Hide toasts after a few seconds.
  document.querySelectorAll(".toast").forEach(function (toast) {
    setTimeout(function () {
      toast.classList.add("is-hiding");
      setTimeout(function () { toast.remove(); }, 400);
    }, 5000);
  });
});
