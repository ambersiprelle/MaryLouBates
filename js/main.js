// Mobile nav toggle
const toggle = document.querySelector('.nav-toggle');
const navLinks = document.querySelector('.nav-links');

if (toggle && navLinks) {
  toggle.addEventListener('click', () => navLinks.classList.toggle('open'));
}

document.querySelectorAll('.nav-links a').forEach(link => {
  link.addEventListener('click', () => navLinks.classList.remove('open'));
});

// Highlight active page
const current = location.pathname.split('/').pop() || 'index.html';
document.querySelectorAll('.nav-links a').forEach(link => {
  const href = link.getAttribute('href');
  if (href === current || (current === '' && href === 'index.html')) {
    link.classList.add('active');
  }
});

// Contact form
const form = document.querySelector('.contact-form form');
if (form) {
  form.addEventListener('submit', e => {
    e.preventDefault();
    form.innerHTML = '<p style="color:var(--coral);font-size:1.1rem;font-weight:600;">Thank you! I\'ll be in touch soon.</p>';
  });
}
