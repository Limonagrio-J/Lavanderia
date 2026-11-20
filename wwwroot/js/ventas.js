(() => {
  const $ = (selector) => document.querySelector(selector);
  const form = $('#venta-form');
  if (!form) return;

  const selProducto = $('#item-producto');
  const inpCantidad = $('#item-cantidad');
  const selCliente = $('#venta-cliente');
  const inpDescuento = $('#venta-descuento');
  const cuerpo = $('#detalle-body');
  const vacio = $('#detalle-vacio');
  const lblTotal = $('#lblTotal');
  const lblTotalTabla = $('#lblTotalTabla');
  const lblSubtotal = $('#lblSubtotal');
  const lblSubtotalTabla = $('#lblSubtotalTabla');
  const lblDescuentoMonto = $('#lblDescuentoMonto');
  const lblDescuentoTabla = $('#lblDescuentoTabla');
  const lblError = $('#venta-error');
  const btnConfirmar = $('#confirmar-button');
  const dialogoOk = $('#venta-ok');

  let detalles = [];

  const dinero = (n) => (Math.round(n * 100) / 100).toLocaleString('en-US',
    { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const escapar = (t) => String(t).replace(/[&<>"']/g,
    (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  function mostrarMensaje(texto, tipo) {
    if (!lblError) return;
    lblError.textContent = texto || '';
    lblError.classList.remove('summary--warn');
    if (tipo === 'warn') lblError.classList.add('summary--warn');
  }

  function subtotalBruto() {
    return detalles.reduce((suma, d) => suma + Math.round(d.cantidad * d.precio * 100), 0) / 100;
  }

  function porcentajeActual() {
    const v = Number(inpDescuento ? inpDescuento.value : 0);
    return Number.isFinite(v) ? v : NaN;
  }

  function descuentoValido(p) {
    return Number.isFinite(p) && p >= 0 && p <= 20;
  }

  function calcularTotales() {
    const sub = subtotalBruto();
    let pct = porcentajeActual();
    if (!descuentoValido(pct)) pct = 0;
    const monto = Math.round(sub * (pct / 100) * 100) / 100;
    const total = Math.round((sub - monto) * 100) / 100;
    return { sub, pct, monto, total };
  }

  function validarDescuentoMostrar() {
    const p = porcentajeActual();
    if (!descuentoValido(p)) {
      mostrarMensaje('El descuento máximo permitido es 20%. Ingresa un valor entre 0% y 20% para continuar.', 'warn');
      return false;
    }
    if (lblError && lblError.classList.contains('summary--warn')) {
      mostrarMensaje('', '');
    }
    return true;
  }

  function pintar() {
    cuerpo.innerHTML = detalles.map((d, i) => `
      <tr>
        <td>${escapar(d.nombre)}</td>
        <td class="num">${d.cantidad}</td>
        <td class="num">Bs. ${dinero(d.precio)}</td>
        <td class="num">Bs. ${dinero(d.cantidad * d.precio)}</td>
        <td class="actions"><button type="button" class="link-button link-button--danger js-quitar" data-i="${i}">Quitar</button></td>
      </tr>`).join('');
    if (vacio) vacio.hidden = detalles.length > 0;

    const { sub, pct, monto, total } = calcularTotales();
    const tSub = `Bs. ${dinero(sub)}`;
    const tMon = `Bs. ${dinero(monto)}`;
    const tTot = `Bs. ${dinero(total)}`;
    if (lblSubtotal) lblSubtotal.textContent = tSub;
    if (lblSubtotalTabla) lblSubtotalTabla.textContent = tSub;
    if (lblDescuentoMonto) lblDescuentoMonto.textContent = tMon;
    if (lblDescuentoTabla) lblDescuentoTabla.textContent = tMon;
    if (lblTotal) lblTotal.textContent = tTot;
    if (lblTotalTabla) lblTotalTabla.textContent = tTot;
  }

  function agregar() {
    mostrarMensaje('', '');
    const opcion = selProducto.selectedOptions[0];
    if (!selProducto.value || !opcion) {
      mostrarMensaje('Selecciona un producto de la lista.', '');
      selProducto.focus();
      return;
    }
    const cantidad = Number(inpCantidad.value);
    if (!Number.isInteger(cantidad) || cantidad < 1 || cantidad > 9999) {
      mostrarMensaje('La cantidad debe ser un número entero entre 1 y 9999. Ej.: 2.', '');
      inpCantidad.focus();
      return;
    }

    const id = Number(selProducto.value);
    const existente = detalles.find((d) => d.id === id);
    if (existente) {
      if (existente.cantidad + cantidad > 9999) {
        mostrarMensaje('La cantidad total de ese producto no puede pasar de 9999.', '');
        return;
      }
      existente.cantidad += cantidad;
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
    if (selCliente) selCliente.value = '0';
    if (selProducto) selProducto.value = '';
    if (inpCantidad) inpCantidad.value = 1;
    if (inpDescuento) inpDescuento.value = '0';
    mostrarMensaje('', '');
    pintar();
  }

  if ($('#agregar-button')) $('#agregar-button').addEventListener('click', agregar);
  if (inpCantidad) inpCantidad.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') { e.preventDefault(); agregar(); }
  });
  if (inpDescuento) {
    inpDescuento.addEventListener('input', () => {
      validarDescuentoMostrar();
      pintar();
    });
  }
  cuerpo.addEventListener('click', (e) => {
    const boton = e.target.closest('.js-quitar');
    if (!boton) return;
    detalles.splice(Number(boton.dataset.i), 1);
    validarDescuentoMostrar();
    pintar();
  });
  if ($('#limpiar-button')) $('#limpiar-button').addEventListener('click', limpiar);
  if ($('#venta-ok-button')) $('#venta-ok-button').addEventListener('click', () => {
    dialogoOk.hidden = true;
    window.location.reload();
  });

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    if (!validarDescuentoMostrar()) return;
    mostrarMensaje('', '');
    if (detalles.length === 0) {
      mostrarMensaje('Agrega al menos un producto antes de confirmar la venta.', '');
      return;
    }

    const pct = porcentajeActual();
    if (!descuentoValido(pct)) {
      mostrarMensaje('El descuento máximo permitido es 20%. Ingresa un valor entre 0% y 20% para continuar.', 'warn');
      return;
    }

    btnConfirmar.disabled = true;
    const textoOriginal = btnConfirmar.textContent;
    btnConfirmar.textContent = 'Guardando...';

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
          idCliente: Number(selCliente ? selCliente.value : 0),
          porcentajeDescuento: pct,
          detalles: detalles.map((d) => ({ idProducto: d.id, cantidad: d.cantidad }))
        })
      });

      if (respuesta.status === 401) {
        mostrarMensaje('Tu sesión expiró. Inicia sesión para continuar.', '');
        return;
      }
      if (!respuesta.ok) throw new Error('http');
      const resultado = await respuesta.json();

      if (resultado.ok) {
        $('#venta-ok-text').textContent = resultado.mensaje;
        detalles = [];
        pintar();
        dialogoOk.hidden = false;
      } else {
        const msg = resultado.mensaje || 'No se pudo registrar la venta.';
        const esDescuento = msg.indexOf('descuento máximo permitido') !== -1 || msg.indexOf('descuento maximo permitido') !== -1;
        mostrarMensaje(msg, esDescuento ? 'warn' : '');
      }
    } catch (err) {
      mostrarMensaje('No pudimos guardar la venta. Revisa tu conexión e inténtalo de nuevo; no se guardó nada.', '');
    } finally {
      btnConfirmar.disabled = false;
      btnConfirmar.textContent = textoOriginal;
    }
  });

  pintar();
})();
