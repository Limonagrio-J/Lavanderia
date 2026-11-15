// Los avisos (toast) enviados por el servidor desaparecen solos
document.addEventListener('DOMContentLoaded', () => {
  const toast = document.getElementById('toast');
  if (toast) {
    setTimeout(() => { toast.hidden = true; }, 4500);
  }
});
