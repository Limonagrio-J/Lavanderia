// Los avisos (toast) enviados por el servidor desaparecen solos
document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('.toast').forEach((toast) => {
    setTimeout(() => { toast.hidden = true; }, 4500);
  });
});
