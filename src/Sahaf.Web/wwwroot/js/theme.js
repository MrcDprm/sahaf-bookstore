// Runs in <head> before the page paints, so the saved theme never flashes.
(function () {
  var theme = null;
  try { theme = localStorage.getItem("sahaf-theme"); } catch (e) { /* storage blocked */ }
  if (theme !== "light" && theme !== "dark") {
    theme = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  }
  document.documentElement.dataset.theme = theme;
})();
