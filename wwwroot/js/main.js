// Mobile nav toggle
const toggle = document.querySelector('.nav-toggle');
const navLinks = document.querySelector('.nav-links');

if (toggle && navLinks) {
  toggle.addEventListener('click', () => navLinks.classList.toggle('open'));
}

document.querySelectorAll('.nav-links a').forEach(link => {
  if (!link.classList.contains('nav-dropdown-toggle')) {
    link.addEventListener('click', () => navLinks.classList.remove('open'));
  }
});

// Dropdown toggle (mobile tap)
document.querySelectorAll('.nav-dropdown-toggle').forEach(btn => {
  btn.addEventListener('click', e => {
    e.preventDefault();
    btn.closest('.nav-dropdown').classList.toggle('open');
  });
});

// Highlight active page
const current = location.pathname.split('/').pop() || 'index.html';
document.querySelectorAll('.nav-links a').forEach(link => {
  const href = link.getAttribute('href');
  if (href === current || (current === '' && href === 'index.html')) {
    link.classList.add('active');
  }
});

// Contact form — server handles POST /contact and redirects with ?contact=ok or ?contact=err
const params = new URLSearchParams(location.search);
const contactStatus = params.get('contact');
const formEl = document.querySelector('.contact-form form');
if (formEl && contactStatus === 'ok') {
  formEl.outerHTML = '<p style="color:var(--coral);font-size:1.1rem;font-weight:600;">Thank you! I\'ll be in touch soon.</p>';
} else if (formEl && contactStatus === 'err') {
  const note = document.createElement('p');
  note.style.cssText = 'color:#c00;font-size:0.95rem;margin-bottom:0.75rem;';
  note.textContent = 'Sorry, something went wrong sending your message. Please try again.';
  formEl.parentNode.insertBefore(note, formEl);
}
