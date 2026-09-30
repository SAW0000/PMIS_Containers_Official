(function () {
  document.addEventListener('contextmenu', function (e) { e.preventDefault(); });

  document.addEventListener('dragstart', function (e) { e.preventDefault(); });

  document.addEventListener('gesturestart', function (e) { e.preventDefault(); });
  document.addEventListener('gesturechange', function (e) { e.preventDefault(); });

  document.addEventListener('wheel', function (e) {
    if (e.ctrlKey) e.preventDefault();
  }, { passive: false });

  document.addEventListener('wheel', function (e) {
    if (e.ctrlKey) return; 
    if (Math.abs(e.deltaX) <= Math.abs(e.deltaY)) return; 

    var el = e.target;
    while (el && el !== document.body) {
      if (el.scrollWidth > el.clientWidth) return; 
      el = el.parentElement;
    }
    e.preventDefault();
  }, { passive: false });

  document.addEventListener('keydown', function (e) {
    if ((e.ctrlKey || e.metaKey) && ['+', '-', '=', '0'].includes(e.key)) {
      e.preventDefault();
    }
  });
})();
