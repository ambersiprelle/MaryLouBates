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

// Book news signup — server redirects with ?news=confirmed|already|unsubscribed|err
const newsStatus = params.get('news');
const newsForm = document.querySelector('.news-form');
const newsMessages = {
  already: "You're already on the list. Thank you &mdash; I'll be in touch when there's news.",
  confirmed: "Thank you! You're on the list. I'll write when there's news about the book.",
  unsubscribed: "You've been unsubscribed. You won't get any more book news from me.",
  err: "Sorry, that didn't go through. Please check your email address and try again."
};
if (newsForm && newsMessages[newsStatus]) {
  const box = document.createElement('p');
  box.className = 'news-status' + (newsStatus === 'err' ? ' err' : '');
  box.setAttribute('role', 'status');
  box.innerHTML = newsMessages[newsStatus];
  if (newsStatus === 'err') newsForm.parentNode.insertBefore(box, newsForm);
  else newsForm.replaceWith(box);
}
