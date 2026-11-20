(() => {
  const $ = (selector) => document.querySelector(selector);
  const dialog = $('#dialog');
  const form = $('#client-form');
  const confirmBox = $('#confirm');
  const baja = $('#baja');
  const summary = $('#summary');
  const submitButton = $('#submit-button');

  const FIELDS = ['Nombre', 'Apellido', 'Documento', 'Telefono', 'Correo',
                  'Direccion', 'TipoCliente', 'FechaRegistro'];

  let initialState = '';
  let opener = null;

  /* ---------- Utilidades ---------- */
  const field = (name) => form.elements.namedItem(name);
  const valueOf = (name) => field(name).value.trim();
  const snapshot = () => JSON.stringify(FIELDS.map((name) => field(name).value));

  function today() {
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    return now.toISOString().slice(0, 10);
  }

  /* ---------- Reglas de validación: mensajes amables y con ejemplo ---------- */
  const rules = {
    Nombre(value) {
      if (!value) return 'Escribe el nombre del cliente. Ej.: María.';
      if (value.length < 2) return 'El nombre es muy corto. Escríbelo completo, por favor.';
      if (/\d/.test(value)) return 'El nombre no debería llevar números. Revisa cómo lo escribiste.';
      return '';
    },
    Apellido(value) {
      if (!value) return 'Escribe el apellido del cliente. Ej.: Quispe.';
      if (value.length < 2) return 'El apellido es muy corto. Escríbelo completo, por favor.';
      if (/\d/.test(value)) return 'El apellido no debería llevar números. Revisa cómo lo escribiste.';
      return '';
    },
    Documento(value) {
      if (!value) return 'Escribe el documento de identidad. Ej.: 10658776.';
      return /^[\w.-]{5,20}$/.test(value)
        ? ''
        : 'Usa de 5 a 20 caracteres (números o letras, sin espacios). Ej.: 10658776.';
    },
    Telefono(value) {
      if (!value) return 'Necesitamos un teléfono para avisar cuando el vehículo esté listo. Ej.: 78445511.';
      if (!/^\+?[\d\s-]+$/.test(value)) {
        return 'Usa solo números (puedes separar con espacios o guiones). Ej.: 784 455 11.';
      }
      const digits = value.replace(/\D/g, '').length;
      return digits < 7 || digits > 15
        ? 'El teléfono debería tener entre 7 y 15 dígitos. Revisa si falta o sobra alguno.'
        : '';
    },
    Correo(value) {
      return !value || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)
        ? ''
        : 'Parece que falta algo en el correo. Ej.: nombre@correo.com (o déjalo vacío).';
    },
    FechaRegistro(value) {
      if (!value) return 'Elige la fecha de registro (por defecto es hoy).';
      return value > today() ? 'La fecha no puede ser futura. Elige hoy o un día anterior.' : '';
    }
  };

  function setError(name, message) {
    const input = field(name);
    $(`#error-${name}`).textContent = message;
    input.classList.toggle('invalid', Boolean(message));
    input.setAttribute('aria-invalid', message ? 'true' : 'false');
  }

  function clearErrors() {
    FIELDS.forEach((name) => setError(name, ''));
    summary.textContent = '';
  }

  function showSummary(total) {
    summary.textContent = total === 1
      ? 'Casi listo: revisa el campo marcado para poder guardar.'
      : `Casi listo: revisa los ${total} campos marcados para poder guardar.`;
  }

  /* ---------- Abrir y cerrar el diálogo ---------- */
  function openDialog(data) {
    const editing = Boolean(data);
    opener = document.activeElement;

    form.reset();
    clearErrors();

    field('Id').value = editing ? data.id : '0';
    field('Nombre').value = editing ? data.nombre : '';
    field('Apellido').value = editing ? data.apellido : '';
    field('Documento').value = editing ? data.documento : '';
    field('Telefono').value = editing ? data.telefono : '';
    field('Correo').value = editing ? (data.correo || '') : '';
    field('Direccion').value = editing ? (data.direccion || '') : '';
    field('TipoCliente').value = editing ? data.tipo : 'Particular'; // valor por defecto
    field('FechaRegistro').value = editing ? data.fecha : today();   // valor por defecto: hoy

    $('#dialog-eyebrow').textContent = editing ? 'Editar registro' : 'Nuevo registro';
    $('#dialog-title').textContent = editing ? 'Editar cliente' : 'Agregar cliente';
    submitButton.textContent = editing ? 'Guardar cambios' : 'Guardar cliente';

    confirmBox.hidden = true;
    dialog.hidden = false;
    document.body.style.overflow = 'hidden';
    initialState = snapshot();
    field('Nombre').focus(); // foco inicial en el primer campo
  }

  function closeDialog() {
    dialog.hidden = true;
    confirmBox.hidden = true;
    document.body.style.overflow = '';
    if (opener) opener.focus();
  }

  function requestClose() {
    if (snapshot() !== initialState) {
      confirmBox.hidden = false;
      $('#keep-button').focus();
    } else {
      closeDialog();
    }
  }

  function keepEditing() {
    confirmBox.hidden = true;
    field('Nombre').focus();
  }

  /* ---------- Guardar ---------- */
  function validateAll() {
    let first = null;
    let total = 0;
    FIELDS.forEach((name) => {
      const message = rules[name] ? rules[name](valueOf(name)) : '';
      setError(name, message);
      if (message) {
        total += 1;
        first = first || name;
      }
    });
    if (total > 0) showSummary(total);
    else summary.textContent = '';
    return first;
  }

  async function submitForm(event) {
    event.preventDefault();
    if (submitButton.disabled) return;

    const firstInvalid = validateAll();
    if (firstInvalid) {
      field(firstInvalid).focus();
      return;
    }

    const label = submitButton.textContent;
    submitButton.disabled = true;
    submitButton.textContent = 'Guardando…';

    try {
      const response = await fetch(form.action, {
        method: 'POST',
        body: new FormData(form),
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
      });
      if (!response.ok) throw new Error('http');
      const result = await response.json();

      if (result.ok) {
        closeDialog();
        window.location.reload(); // la lista se vuelve a cargar y aparece el aviso de éxito
        return;
      }

      // Errores que detectó el servidor (por ejemplo, documento repetido)
      const names = Object.keys(result.errores || {});
      names.forEach((name) => { if (FIELDS.includes(name)) setError(name, result.errores[name]); });
      if (names.length > 0) {
        showSummary(names.length);
        const first = names.find((name) => FIELDS.includes(name));
        if (first) field(first).focus();
      } else {
        summary.textContent = result.general || 'No pudimos guardar los datos. Inténtalo de nuevo en un momento.';
      }
    } catch {
      summary.textContent = 'No pudimos guardar los datos en este momento. Revisa tu conexión e inténtalo de nuevo.';
    } finally {
      submitButton.disabled = false;
      submitButton.textContent = label;
    }
  }

  /* ---------- Confirmación de baja ---------- */
  function openBaja(id, name) {
    opener = document.activeElement;
    $('#baja-id').value = id;
    $('#baja-name').textContent = name;
    baja.hidden = false;
    document.body.style.overflow = 'hidden';
    $('#baja-cancel').focus(); // la opción segura queda enfocada
  }

  function closeBaja() {
    baja.hidden = true;
    document.body.style.overflow = '';
    if (opener) opener.focus();
  }

  /* ---------- Teclado: Esc, Ctrl+S y foco atrapado ---------- */
  function trapFocus(scope, event) {
    const items = [...scope.querySelectorAll('button, input, select')]
      .filter((el) => !el.disabled && el.offsetParent !== null);
    if (!items.length) return;

    const first = items[0];
    const last = items[items.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  document.addEventListener('keydown', (event) => {
    if (!baja.hidden) {
      if (event.key === 'Escape') { event.preventDefault(); closeBaja(); }
      else if (event.key === 'Tab') trapFocus(baja, event);
      return;
    }
    if (dialog.hidden) return;

    if (event.key === 'Escape') {
      event.preventDefault();
      if (confirmBox.hidden) requestClose();
      else keepEditing();
    } else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
      event.preventDefault();
      if (confirmBox.hidden) form.requestSubmit();
    } else if (event.key === 'Tab') {
      trapFocus(confirmBox.hidden ? form : confirmBox, event);
    }
  });

  /* ---------- Conexiones ---------- */
  $('#open-button').addEventListener('click', () => openDialog(null));
  $('#close-button').addEventListener('click', requestClose);
  $('#cancel-button').addEventListener('click', requestClose);
  $('#keep-button').addEventListener('click', keepEditing);
  $('#discard-button').addEventListener('click', closeDialog);
  $('#baja-cancel').addEventListener('click', closeBaja);
  form.addEventListener('submit', submitForm);

  document.addEventListener('click', (event) => {
    const edit = event.target.closest('.js-edit');
    if (edit) { openDialog(edit.dataset); return; }

    const remove = event.target.closest('.js-baja');
    if (remove) openBaja(remove.dataset.id, remove.dataset.nombre);
  });

  // Un error desaparece apenas el usuario empieza a corregir ese campo
  FIELDS.forEach((name) => {
    field(name).addEventListener('input', () => {
      setError(name, '');
      summary.textContent = '';
    });
  });
})();
