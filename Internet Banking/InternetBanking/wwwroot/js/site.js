(function () {
  const root = document.documentElement;
  const themeToggle = document.getElementById('themeToggle');
  const sidebar = document.getElementById('appSidebar');
  const sidebarToggle = document.getElementById('sidebarToggle');
  const backdrop = document.getElementById('sidebarBackdrop');

  function setTheme(theme) {
    if (theme === 'dark') {
      root.setAttribute('data-theme', 'dark');
    } else {
      root.removeAttribute('data-theme');
    }
    localStorage.setItem('ib-theme', theme);
    if (themeToggle) {
      themeToggle.innerHTML = theme === 'dark'
        ? '<i class="bi bi-sun-fill"></i>'
        : '<i class="bi bi-moon-stars"></i>';
    }
  }

  function openSidebar() {
    if (!sidebar || !backdrop) return;
    sidebar.classList.add('open');
    backdrop.classList.add('show');
  }

  function closeSidebar() {
    if (!sidebar || !backdrop) return;
    sidebar.classList.remove('open');
    backdrop.classList.remove('show');
  }

  if (themeToggle) {
    const existing = localStorage.getItem('ib-theme') === 'dark' ? 'dark' : 'light';
    setTheme(existing);
    themeToggle.addEventListener('click', function () {
      const next = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
      setTheme(next);
    });
  }

  if (sidebarToggle) {
    sidebarToggle.addEventListener('click', openSidebar);
  }

  if (backdrop) {
    backdrop.addEventListener('click', closeSidebar);
  }

  window.addEventListener('resize', function () {
    if (window.innerWidth >= 1024) {
      closeSidebar();
    }
  });
})();
