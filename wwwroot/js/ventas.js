(() => {
  const $ = (selector) => document.querySelector(selector);
  const form = $('#venta-form');
  const selProducto = $('#item-producto');
  const inpCantidad = $('#item-cantidad');
  const selCliente = $('#venta-cliente');
  const cuerpo = $('#detalle-body');
  const vacio = $('#detalle-vacio');
  const lblTotal = $('#lblTotal');
  const lblTotalTabla = $('#lblTotalTabla');
  const lblError = $('#venta-error');
  const btnConfirmar = $('#confirmar-button');
  const dialogoOk = $('#venta-ok');

  // Lista de ítems de la venta actual (en memoria)
  let detalles = [];

  const dinero = (n) => (Math.round(n * 100) / 100).toLocaleString('en-US',
    { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const escapar = (t) => t.replace(/[&<>"']/g,
    (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  function mostrarError(texto) { lblError.textContent = texto || ''; }

  function total() {
    return detalles.reduce((suma, d) => suma + Math.round(d.cantidad * d.precio * 100), 0) / 100;
  }

  // Redibuja la tabla y actualiza el total
  function pintar() {
    cuerpo.innerHTML = detalles.map((d, i) => `
      <tr>
        <td>${escapar(d.nombre)}</td>
        <td class="num">${d.cantidad}</td>
        <td class="num">Bs. ${dinero(d.precio)}</td>
        <td class="num">Bs. ${dinero(d.cantidad * d.precio)}</td>
        <td class="actions"><button type="button" class="link-button link-button--danger js-quitar" data-i="${i}">Quitar</button></td>
      </tr>`).join('');
    vacio.hidden = detalles.length > 0;
    const texto = `Bs. ${dinero(total())}`;
    lblTotal.textContent = texto;
    lblTotalTabla.textContent = texto;
  }

  function agregar() {
    mostrarError('');
    const opcion = selProducto.selectedOptions[0];
    if (!selProducto.value || !opcion) {
      mostrarError('Selecciona un producto de la lista.');
      selProducto.focus();
      return;
    }
    const cantidad = Number(inpCantidad.value);
    if (!Number.isInteger(cantidad) || cantidad < 1 || cantidad > 9999) {
      mostrarError('La cantidad debe ser un número entero entre 1 y 9999. Ej.: 2.');
      inpCantidad.focus();
      return;
    }

    const id = Number(selProducto.value);
    const existente = detalles.find((d) => d.id === id);
    if (existente) {
      if (existente.cantidad + cantidad > 9999) {
        mostrarError('La cantidad total de ese producto no puede pasar de 9999.');
        return;
      }
      existente.cantidad += cantidad; // mismo producto: se suman las cantidades
    } else {
      detalles.push({
        id,
        nombre: opcion.dataset.nombre,
        precio: Number(opcion.dataset.precio),
        cantidad
      });
    }

    selProducto.value = '';
    inpCantidad.value = 1;
    pintar();
    selProducto.focus();
  }

  function limpiar() {
    detalles = [];
    selCliente.value = '0';
    selProducto.value = '';
    inpCantidad.value = 1;
    mostrarError('');
    pintar();
  }

  $('#agregar-button').addEventListener('click', agregar);
  inpCantidad.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') { e.preventDefault(); agregar(); }
  });
  cuerpo.addEventListener('click', (e) => {
    const boton = e.target.closest('.js-quitar');
    if (!boton) return;
    detalles.splice(Number(boton.dataset.i), 1);
    mostrarError('');
    pintar();
  });
  $('#limpiar-button').addEventListener('click', limpiar);
  $('#venta-ok-button').addEventListener('click', () => {
    dialogoOk.hidden = true;
    window.location.reload(); // nueva venta con la fecha actualizada
  });

  // Confirmar: valida que haya ítems y envía la venta completa al servidor
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    mostrarError('');
    if (detalles.length === 0) {
      mostrarError('Agrega al menos un producto antes de confirmar la venta.');
      return;
    }

    btnConfirmar.disabled = true;
    const textoOriginal = btnConfirmar.textContent;
    btnConfirmar.textContent = 'Guardando…';

    try {
      const tokenField = form.querySelector('input[name="__RequestVerificationToken"]');
      const token = tokenField ? tokenField.value : '';
      const respuesta = await fetch(form.action, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'RequestVerificationToken': token,
          'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify({
          idCliente: Number(selCliente.value),
          detalles: detalles.map((d) => ({ idProducto: d.id, cantidad: d.cantidad }))
        })
      });
      if (!respuesta.ok) throw new Error('http');
      const resultado = await respuesta.json();

      if (resultado.ok) {
        $('#venta-ok-text').textContent = resultado.mensaje;
        detalles = [];
        pintar();
        dialogoOk.hidden = false;
      } else {
        mostrarError(resultado.mensaje);
      }
    } catch (err) {
      mostrarError('No pudimos guardar la venta. Revisa tu conexión e inténtalo de nuevo; no se guardó nada.');
    } finally {
      btnConfirmar.disabled = false;
      btnConfirmar.textContent = textoOriginal;
    }
  });

  pintar();
})();
