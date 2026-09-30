function sharedShowToast(msg, type, spinnerIcon){
  const el = document.createElement('div');
  el.className = 'toast';
  const isLoading = type === true || type === 'loading';
  let icon;
  if (isLoading) {
    icon = spinnerIcon;
  } else if (type === 'warning') {
    icon = TOAST_ICONS.warning;
  } else {
    icon = TOAST_ICONS.success;
  }
  el.innerHTML = `${icon}<span>${msg}</span>`;
  document.getElementById('toastWrap').appendChild(el);
  const dismiss = () => { el.style.opacity='0'; el.style.transition='.3s'; setTimeout(()=>el.remove(),300); };
  const timer = setTimeout(dismiss, 3200);
  return () => { clearTimeout(timer); dismiss(); };
}

const TOAST_ICONS = {
  success: '<svg viewBox="0 0 24 24" width="17" height="17"><path fill="none" stroke="currentColor" stroke-dasharray="26" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 11l6 6l10 -10"><animate fill="freeze" attributeName="stroke-dashoffset" dur="0.6s" values="26;0"/></path></svg>',
  warning: '<svg viewBox="0 0 16 16" width="16" height="16"><path d="M0 0h16v16H0z" fill="none"/><path fill="currentColor" d="M7.002 11a1 1 0 1 1 2 0a1 1 0 0 1-2 0M7.1 4.995a.905.905 0 1 1 1.8 0l-.35 3.507a.553.553 0 0 1-1.1 0z"/></svg>'
};
