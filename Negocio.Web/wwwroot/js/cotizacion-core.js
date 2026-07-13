// Usamos var o inicialización en window para evitar el SyntaxError si el script se procesa dos veces
if (typeof window.activeDraggingContainer === 'undefined') {
    window.activeDraggingContainer = null;
}
window.Cotizacion = (function () {

    let config = {};
    let productoId = null;
    let dimensionSeleccionada = null;
    let tuboSeleccionado = null;
    let accesorios = [];

    /* ============================
       INIT
    ============================ */

    function init(userConfig) {
        config = userConfig || {};

        document.addEventListener('DOMContentLoaded', () => {

            bindEventosBase();
            initTipoVenta();
            initMedidas(config.maxMetros || 5.95);
            initReserva();

            
        });
    }

    /* ============================
    PRODUCTOS
    ============================ */
    async function cargarProductos() {
        let url = config.urls.getProductos;
        const params = new URLSearchParams();

        if (typeof config.getMaterial === "function") {
            const material = config.getMaterial();
            if (material) params.append("material", material);
        }

        if (typeof config.getSeccion === "function") {
            const seccion = config.getSeccion();
            if (seccion) params.append("seccion", seccion);
        }

        if ([...params].length > 0) {
            url += "?" + params.toString();
        }

        const cont =
            document.getElementById('contenedorTubos');

        if (!cont) return;

        let data;

        try {
            const res = await fetch(url);

            if (!res.ok) {
                throw new Error(
                    `Error al cargar productos: HTTP ${res.status}`
                );
            }

            data = await res.json();

            if (!Array.isArray(data)) {
                throw new Error(
                    'La respuesta de productos no tiene un formato válido.'
                );
            }
        } catch (error) {
            console.error(
                'Error al cargar productos:',
                error
            );

            cont.innerHTML = `
        <div class="text-center text-danger py-4">
            No se pudieron cargar los productos.
            Intenta nuevamente.
        </div>
    `;

            return;
        }

        cont.innerHTML = '';

        if (!data || data.length === 0) {
            cont.innerHTML = '<div class="text-center text-muted py-4">No hay tubos disponibles para este material</div>';
            return;
        }

        // 🧠 Agrupamos variantes por descripción
        const productosAgrupados = {};
        data.forEach(p => {
            if (!productosAgrupados[p.descripcion]) {
                productosAgrupados[p.descripcion] = {
                    descripcion: p.descripcion,
                    imagen: p.imagen,
                    variantes: []
                };
            }
            productosAgrupados[p.descripcion].variantes.push(p);
        });

        const primerGrupoKey = Object.keys(productosAgrupados)[0];
        const productoMaestro = productosAgrupados[primerGrupoKey];

        // 🏗️ Maquetación Ultra-UX: Carrusel horizontal arriba en móvil, clásico a la derecha en PC
        cont.innerHTML = `
            <!-- COLUMNA DE DIÁMETROS (En móvil sube y se vuelve carrusel; en PC va a la derecha) -->
            <div class="col-12 col-md-6 order-1 order-md-2 d-flex align-items-center mb-3 mb-md-0">
                <div class="p-2 w-100 container-pills-wrapper">
                    <span class="text-secondary fw-bold small text-uppercase d-block mb-2 mb-md-3 text-center text-md-start" style="letter-spacing: 0.5px; font-size: 0.8rem;">
                        DimensiónES / Diámetros disponibles:
                    </span>
                    <!-- Contenedor con scroll horizontal táctil nativo en móviles -->
                    <div class="d-flex flex-row flex-md-wrap gap-2 idat-carrusel-pills justify-content-md-start" id="contenedorDimensionesPills">
                    </div>
                </div>
            </div>

            <!-- COLUMNA DE IMAGEN MAESTRA (En móvil baja para no tapar opciones; en PC va a la izquierda) -->
            <div class="col-12 col-md-6 order-2 order-md-1 text-center border-end-md">
                <div class="p-3 d-flex flex-column align-items-center justify-content-center" style="min-height: 380px;">

                    <div class="position-relative w-100 d-flex align-items-center justify-content-center mb-4" style="min-height: 28px; max-height: 280px;">
                        <img id="masterTuboImg" src="${productoMaestro.imagen ?? '/img/home/placeholder.png'}" 
                             class="img-fluid custom-master-img" style="max-height: 260px; object-fit: contain;">
                
                        <div id="loaderTuboInterno" class="position-absolute top-0 start-0 w-100 h-100 d-none flex-column align-items-center justify-content-center rounded-4" 
                             style="background: rgba(255,255,255,0.85); z-index: 5;">
                            <div class="spinner-border text-success" role="status" style="width: 3rem; height: 3rem;"></div>
                            <span class="text-dark fw-bold mt-2 small text-uppercase" style="letter-spacing: 0.5px;">Cargando dimensión...</span>
                        </div>
                    </div>
            
                    <h3 id="masterTuboTitulo" class="fw-extrabold text-dark text-uppercase mb-3 mb-md-4" style="font-size: 1.25rem; letter-spacing: 0.5px;">
                        ${productoMaestro.descripcion}
                    </h3>
            
                    <button type="button" class="btn btn-success px-5 py-2.5 fw-bold btnConfirmarTuboMaster" 
                            style="font-size: 1.05rem; background-color: #16a34a; border: none; border-radius: 50px; box-shadow: 0 4px 12px rgba(22, 163, 74, 0.2);">
                         Confirmar tubo
                    </button>
                </div>
            </div>
        `;

        const contenedorPills = document.getElementById('contenedorDimensionesPills');
        let varianteTemporalSeleccionada = null;

        productoMaestro.variantes.forEach((v, index) => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'btn-dimension-pill';
            btn.innerText = v.dimension ? v.dimension : "Estándar";

            btn.dataset.id = v.idProducto;
            btn.dataset.dimension = v.dimension;
            btn.dataset.descripcion = v.descripcion;
            btn.dataset.imagen = v.imagen ?? '/img/home/placeholder.png';

            // Al hacer clic en una píldora activamos la carga y actualizamos el botón de confirmación
            btn.addEventListener('click', function () {
                contenedorPills.querySelectorAll('.btn-dimension-pill').forEach(b => b.classList.remove('active'));
                this.classList.add('active');

                // Guardamos la referencia temporal de los datos seleccionados
                varianteTemporalSeleccionada = {
                    id: this.dataset.id,
                    dimension: this.dataset.dimension,
                    descripcion: this.dataset.descripcion
                };

                // Capturamos el botón, el loader interno y la imagen
                // ⚡ REPARACIÓN DE SELECTOR GLOBAL: Buscamos en document porque el botón ya se mudó abajo
                const btnConfirmar = document.querySelector('.btnConfirmarTuboMaster');
                const loaderInterno = document.getElementById('loaderTuboInterno');
                const masterImg = document.getElementById('masterTuboImg');
                const dimensionTexto = this.dataset.dimension ? this.dataset.dimension : "Estándar";

                // 🔒 SEGURO ANTI-INTERNET LENTO: Deshabilitamos el botón y le ponemos spinner de espera
                if (btnConfirmar) {
                    btnConfirmar.disabled = true;
                    btnConfirmar.style.opacity = "0.7";
                    btnConfirmar.innerHTML = `<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span> Actualizando...`;
                }

                if (loaderInterno && masterImg) {
                    loaderInterno.classList.remove('d-none');
                    loaderInterno.classList.add('d-flex');
                    masterImg.style.transform = 'scale(0.95)';

                    setTimeout(() => {
                        masterImg.src = this.dataset.imagen;
                        masterImg.style.transform = 'scale(1)';
                        loaderInterno.classList.remove('d-flex');
                        loaderInterno.classList.add('d-none');

                        // 🔓 LIBERACIÓN: Cuando la imagen ya cambió, activamos el botón con su texto definitivo
                        if (btnConfirmar) {
                            btnConfirmar.disabled = false;
                            btnConfirmar.style.opacity = "1";
                            btnConfirmar.innerHTML = `Confirmar Tubo de ${dimensionTexto} <i class="bi bi-arrow-right-short ms-1"></i>`;
                        }
                    }, 600); // Sincronizado con los 600ms del micro-loader de la tarjeta
                }
            });

            contenedorPills.appendChild(btn);

            // Forzar clic inicial para rellenar la UI por defecto
            if (index === 0) btn.click();
        });

        // 🚀 LÓGICA DEL BOTÓN SELECCIONAR:
        cont.querySelector('.btnConfirmarTuboMaster').addEventListener('click', () => {
            if (!varianteTemporalSeleccionada) return;

            // Seteamos las variables del State en cotizacion-core.js
            productoId = varianteTemporalSeleccionada.id;
            dimensionSeleccionada = varianteTemporalSeleccionada.dimension;
            tuboSeleccionado = varianteTemporalSeleccionada.descripcion;

            // Disparamos tu evento original nativo
            document.dispatchEvent(new CustomEvent('productoSeleccionado', {
                detail: {
                    productoId,
                    dimension: dimensionSeleccionada
                }
            }));
        });

        // 🚀 TRUCO DE MUDACIÓN LIMPIO (SIN DUPLICADOS)
        const botonReal = document.querySelector('.btnConfirmarTuboMaster');
        const destinoSticky = document.getElementById('columnaBotonConfirmarMaster');

        if (botonReal && destinoSticky) {
            destinoSticky.innerHTML = ''; // 🧹 Limpia cualquier botón viejo antes de meter el nuevo
            destinoSticky.appendChild(botonReal);
        }
    }
    function seleccionarProducto(card) {

        document.querySelectorAll('.tubo-card')
            .forEach(c => c.classList.remove('active'));

        card.classList.add('active');

        productoId = card.dataset.id;
        dimensionSeleccionada = card.dataset.dimension;
        tuboSeleccionado = card.dataset.descripcion;

        // 👇 DISPARAR EVENTO
        document.dispatchEvent(new CustomEvent('productoSeleccionado', {
            detail: {
                productoId,
                dimension: dimensionSeleccionada
            }
        }));
    }

    /* ============================
       TIPO DE VENTA
    ============================ */

    function initTipoVenta() {

        document.querySelectorAll('input[name="tipoVenta"]')
            .forEach(r => {
                r.addEventListener('change', actualizarVistaTipoVenta);
            });

        actualizarVistaTipoVenta();
    }

    function actualizarVistaTipoVenta() {

        const tipo = getTipoVenta();

        const grupoLargo = document.getElementById('grupoLargo');
        const grupoVisual = document.getElementById('grupoVisual');
        const grupoCantidad = document.getElementById('grupoCantidad');

        if (!grupoLargo) return;

        const esVarilla = tipo === 'VARILLA';

        grupoLargo.style.display = esVarilla ? 'none' : 'block';

        if (grupoVisual)
            grupoVisual.style.display = esVarilla ? 'none' : 'block';

        if (grupoCantidad)
            grupoCantidad.classList.toggle('d-none', !esVarilla);
    }

    function getTipoVenta() {
        return document.querySelector(
            'input[name="tipoVenta"]:checked'
        )?.value || 'METRO';
    }

    /* ============================
       MEDIDAS
    ============================ */

    function initMedidas(maxMetros = 5.95) {

        const largoInput = document.getElementById('largo');
        const metrosInput = document.getElementById('metrosInput');
        const cmInput = document.getElementById('cmInput');

        if (!largoInput) return;

        function actualizarDesdeTotal(total) {

            if (total < 0.1) total = 0.1;
            if (total > 5.95) total = 5.95; // Tope estricto

            largoInput.value = total.toFixed(2);

            const lbl = document.getElementById('lblMetros');
            const visual = document.getElementById('tuboVisual');

            if (lbl) lbl.innerText = total.toFixed(2);
            if (visual) visual.style.width = (total / 5.95) * 100 + "%";
        }

        // 👇 Si existen metros y cm
        if (metrosInput && cmInput) {

            function actualizar() {
                let metros = parseInt(metrosInput.value) || 0;
                let cm = parseInt(cmInput.value) || 0;
                let total = metros + (cm / 100);
                actualizarDesdeTotal(total);
            }

            metrosInput.addEventListener('input', actualizar);
            cmInput.addEventListener('input', actualizar);

            actualizar();
        }

        // 👇 Si solo existe largo
        largoInput.addEventListener('input', () => {
            actualizarDesdeTotal(parseFloat(largoInput.value) || 0);
        });
    }

    /* ============================
       ACCESORIOS
    ============================ */

    // En cotizacion-core.js
    function agregarAccesorio(productoId, descripcion, cantidad, dimension) { // 👈 Agregamos 'dimension'
        const existente = accesorios.find(a => a.productoId === productoId);

        if (existente) {
            existente.cantidad += cantidad;
            // Opcional: actualizar dimensión si fuera necesario
            existente.dimension = dimension;
        } else {
            // ✅ Ahora guardamos la dimensión dentro del objeto accesorio
            accesorios.push({
                productoId,
                descripcion,
                cantidad,
                dimension // 👈 Esta es la clave para el JSON
            });
        }

        renderAccesorios();
    }

    function renderAccesorios() {
        const contModal = document.getElementById('listaAccesoriosModal');
        const badgeContador = document.getElementById('badgeContadorAcc');
        const burbuja = document.getElementById('btnCarritoFlotante');

        if (!contModal) return;

        // 🧠 CORRECCIÓN 1: El contador ahora cuenta por FILA (items únicos en la lista)
        let totalFilas = accesorios.length;
        if (badgeContador) badgeContador.innerText = totalFilas;

        // Efecto de pulso en la burbuja al insertar
        if (burbuja && totalFilas > 0) {
            burbuja.classList.add('burbuja-pop');
            setTimeout(() => burbuja.classList.remove('burbuja-pop'), 400);
        }

        if (!accesorios.length) {
            contModal.innerHTML = '<div class="text-center text-muted py-3">Ningún accesorio incluido aún</div>';
            return;
        }

        // 🧠 CORRECCIÓN 2: Reemplazo del botón por una "✕" roja y visible
        contModal.innerHTML = `
        <div class="d-flex flex-column gap-2">
            ${accesorios.map((a, i) => `
                <div class="d-flex justify-content-between align-items-center bg-white p-3 rounded-3 shadow-sm border">
                    <div class="text-start">
                        <strong class="text-dark text-uppercase d-block" style="font-size:0.85rem;">${a.descripcion}</strong>
                        <small class="text-muted" style="font-size:0.75rem;">Dim: ${a.dimension ?? 'Estándar'}</small>
                    </div>
                    <div class="d-flex align-items-center gap-3">
                        <span class="badge bg-success-subtle text-success fw-bold px-2.5 py-1.5" style="font-size:0.85rem;">x ${a.cantidad}</span>
                        
                        <button type="button" class="btn btn-sm fw-bold d-flex align-items-center justify-content-center" 
                                onclick="Cotizacion.quitarAccesorio(${i})" 
                                style="width: 28px; height: 28px; border-radius: 50%; border: 1px solid #ef4444; background: #fef2f2; color: #ef4444; font-size: 0.85rem; padding: 0; line-height: 1;">
                            ✕
                        </button>
                    </div>
                </div>
            `).join('')}
        </div>
    `;
    }

    function quitarAccesorio(i) {
        accesorios.splice(i, 1);
        renderAccesorios();
    }

    function getAccesorios() {
        return accesorios;
    }

    /* ============================
       CALCULAR
    ============================ */

    function bindEventosBase() {

        document.getElementById('btnCalcular')
            ?.addEventListener('click', calcular);
    }

    async function calcular() {

        if (!config.urls?.calcular) return;

        if (!productoId) {
            alert("Selecciona un producto primero");
            return;
        }

        const btnCalcular =
            document.getElementById('btnCalcular');

        const loadingElement =
            document.getElementById(
                'loadingCotizacionModal'
            );

        const modalCarga =
            loadingElement &&
                typeof bootstrap !== 'undefined'
                ? bootstrap.Modal.getOrCreateInstance(
                    loadingElement
                )
                : null;

        

        try {
            if (btnCalcular) {
                btnCalcular.disabled = true;
            }

            modalCarga?.show();

            // 🔥 Payload base (el que usa cortina)
            const basePayload = {
                tuboProductoId: Number(productoId),
                tipoVenta: getTipoVenta(),
                largoMetros: parseFloat(
                    document.getElementById('largo')
                        ?.value || 0
                ),
                cantidadPiezas: parseInt(
                    document.getElementById('piezas')
                        ?.value || 1,
                    10
                ),
                accesorios: accesorios.map(a => ({
                    productoId: Number(a.productoId),
                    cantidad: Number(a.cantidad)
                }))
            };

            // 🔥 Si el módulo define un mapper, lo usamos
            const payload =
                typeof config.mapPayload === "function"
                    ? config.mapPayload(basePayload)
                    : basePayload;

            const res = await fetch(
                config.urls.calcular,
                {
                    method: 'POST',
                    headers: {
                        'Content-Type':
                            'application/json',
                        'X-Requested-With':
                            'XMLHttpRequest'
                    },
                    body: JSON.stringify(payload)
                }
            );

            const textoRespuesta =
                await res.text();

            let resultado = null;

            if (textoRespuesta) {
                try {
                    resultado =
                        JSON.parse(textoRespuesta);
                } catch {
                    resultado = null;
                }
            }

            if (!res.ok) {
                const mensajeServidor =
                    resultado?.mensaje ||
                    resultado?.message ||
                    textoRespuesta ||
                    'No se pudo calcular la cotización.';

                throw new Error(mensajeServidor);
            }

            if (
                !resultado ||
                typeof resultado !== 'object'
            ) {
                throw new Error(
                    'El servidor devolvió una respuesta de cálculo no válida.'
                );
            }

            document.dispatchEvent(
                new CustomEvent(
                    'mostrarResultado',
                    {
                        detail: resultado
                    }
                )
            );

        } catch (error) {
            console.error(
                'Error al calcular la cotización:',
                error
            );

            alert(
                error.message ||
                'No se pudo calcular la cotización. Inténtalo nuevamente.'
            );

        } finally {
            modalCarga?.hide();

            if (btnCalcular) {
                btnCalcular.disabled = false;
            }
        }
    }

    /* ============================
       RESERVA
    ============================ */

    function initReserva() {
        document.getElementById('btnReservar')?.addEventListener('click', async () => {
            // 🕵️‍♂️ Verificamos la variable global de sesión inyectada por Razor
            if (typeof idUsuarioLogueado !== 'undefined' && idUsuarioLogueado !== null) {
                try {
                    // 🚀 Hacemos la consulta en caliente al endpoint del cliente
                    const respuestaPerfil = await fetch('/Cliente/ObtenerPerfilJson');
                    if (respuestaPerfil.ok) {
                        const datosCliente = await respuestaPerfil.json();

                        // 🧠 Llenado automático inteligente de campos si el usuario está logueado
                        const inputNombre = document.getElementById('clienteNombre');
                        const inputTelefono = document.getElementById('clienteTelefono');
                        const inputCorreo = document.getElementById('clienteCorreo');

                        if (inputNombre && datosCliente.nombreCompleto) inputNombre.value = datosCliente.nombreCompleto;
                        if (inputTelefono && datosCliente.telefono) inputTelefono.value = datosCliente.telefono;
                        if (inputCorreo && datosCliente.correo) inputCorreo.value = datosCliente.correo;

                        inputNombre?.dispatchEvent(
                            new Event('input', { bubbles: true })
                        );

                        inputTelefono?.dispatchEvent(
                            new Event('input', { bubbles: true })
                        );
                    }
                } catch (err) {
                    console.warn("No se pudo auto-llenar el perfil en caliente, procediendo normal:", err);
                }
            }

            // Abrimos el modal tradicionalmente de forma limpia
            const modalReserva =
                document.getElementById('reservaModal');

            if (modalReserva) {
                bootstrap.Modal
                    .getOrCreateInstance(modalReserva)
                    .show();
            }
        });

        // Control de visibilidad: Recojo vs Delivery vs Tren (se mantiene intacto)
        

        document.getElementById('btnConfirmarReserva')?.addEventListener('click', confirmarReserva);
    }

    async function confirmarReserva() {
        if (!config.urls?.reserva) return;

        const btnConfirmar =
            document.getElementById('btnConfirmarReserva');

        const nombre =
            document.getElementById('clienteNombre')
                ?.value.trim() || '';

        const telefono =
            document.getElementById('clienteTelefono')
                ?.value.trim() || '';

        const correo =
            document.getElementById('clienteCorreo')
                ?.value.trim() || '';

        const tipoEntrega =
            document.getElementById('clienteEntrega')
                ?.value || 'RECOJO';

        const tipoFecha =
            document.getElementById('tipoFecha')
                ?.value || 'HOY';

        const clienteFecha =
            document.getElementById('clienteFecha')
                ?.value || '';

        /* ============================
           VALIDACIÓN BÁSICA
        ============================ */

        if (!nombre) {
            alert('⚠️ Ingresa tu nombre completo.');
            return;
        }

        if (!/^\d{9}$/.test(telefono)) {
            alert('⚠️ Ingresa un teléfono válido de 9 dígitos.');
            return;
        }

        if (
            correo &&
            !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(correo)
        ) {
            alert('⚠️ Ingresa un correo válido.');
            return;
        }

        /* ============================
           CONSTRUIR DIRECCIÓN
        ============================ */

        let direccionFinal = '';

        if (tipoEntrega === 'RECOJO') {
            direccionFinal =
                'RECOJO EN TIENDA (CANTAGALLO)';
        }

        else if (tipoEntrega === 'ESTACION') {
            const estacion =
                document.getElementById('estacionTren')
                    ?.value || '';

            if (!estacion) {
                alert('⚠️ Selecciona una estación.');
                return;
            }

            direccionFinal =
                `ENTREGA EN ESTACION: ${estacion}`;
        }

        else if (tipoEntrega === 'DELIVERY') {
            const comboDireccion =
                document.getElementById(
                    'comboDireccionesFrecuentes'
                );

            const direccionFrecuente =
                comboDireccion?.value || '';

            const estaLogueado =
                typeof idUsuarioLogueado !== 'undefined' &&
                idUsuarioLogueado !== null;

            if (
                estaLogueado &&
                direccionFrecuente &&
                direccionFrecuente !== 'MANUAL'
            ) {
                direccionFinal = direccionFrecuente;
            } else {
                const distrito =
                    document.getElementById('manualDistrito')
                        ?.value.trim() || '';

                const direccion =
                    document.getElementById(
                        'manualDireccionTexto'
                    )?.value.trim() || '';

                const referencia =
                    document.getElementById(
                        'manualReferencia'
                    )?.value.trim() || '';

                if (!distrito || !direccion || !referencia) {
                    alert(
                        '⚠️ Completa distrito, dirección y referencia.'
                    );
                    return;
                }

                direccionFinal =
                    `DOMICILIO: ${distrito} - ${direccion} ` +
                    `(Ref: ${referencia})`;
            }
        }

        else if (tipoEntrega === 'SHALOM') {
            const comboAgencias =
                document.getElementById(
                    'comboAgenciasFrecuentes'
                );

            const agenciaFrecuente =
                comboAgencias?.value || '';

            const estaLogueado =
                typeof idUsuarioLogueado !== 'undefined' &&
                idUsuarioLogueado !== null;

            if (
                estaLogueado &&
                agenciaFrecuente &&
                agenciaFrecuente !== 'MANUAL'
            ) {
                direccionFinal = agenciaFrecuente;
            } else {
                const agencia =
                    document.getElementById(
                        'clienteShalomAgencia'
                    )?.value.trim() || '';

                const receptor =
                    document.getElementById(
                        'clienteShalomReceptor'
                    )?.value.trim() || '';

                if (!agencia || !receptor) {
                    alert(
                        '⚠️ Completa la agencia Shalom y los datos de quien recogerá.'
                    );
                    return;
                }

                direccionFinal =
                    `AGENCIA SHALOM: ${agencia} | ` +
                    `RECOGE: ${receptor}`;
            }
        }

        /* ============================
           FECHA SOLICITADA
        ============================ */

        let fechaSolicitada;

        if (tipoFecha === 'FECHA') {
            if (!clienteFecha) {
                alert(
                    '⚠️ Selecciona la fecha en que necesitas el pedido.'
                );
                return;
            }

            fechaSolicitada =
                new Date(
                    `${clienteFecha}T12:00:00`
                ).toISOString();
        } else {
            fechaSolicitada = new Date().toISOString();
        }

        /* ============================
           DATOS DEL COTIZADOR
        ============================ */

        const inputLargo =
            document.getElementById('largo');

        const inputPiezas =
            document.getElementById('piezas');

        const metrosFinales =
            parseFloat(inputLargo?.value || '1');

        const cantidadCombos =
            parseInt(inputPiezas?.value || '1', 10);

        if (!productoId) {
            alert('⚠️ No se encontró el tubo seleccionado.');
            return;
        }

        if (
            !Number.isFinite(metrosFinales) ||
            metrosFinales <= 0
        ) {
            alert('⚠️ La medida seleccionada no es válida.');
            return;
        }

        if (
            !Number.isInteger(cantidadCombos) ||
            cantidadCombos <= 0
        ) {
            alert('⚠️ La cantidad de combos no es válida.');
            return;
        }

        const detalle = {
            tipo: 'COTIZACION_CORTINAS',
            productoId: Number(productoId),
            tubo: tuboSeleccionado,
            dimension: dimensionSeleccionada,
            tipoVenta: getTipoVenta() || 'METRO',
            metrosTotales: metrosFinales,
            cantidadPiezas: cantidadCombos,
            accesorios: accesorios.map(a => ({
                productoId: Number(a.productoId),
                descripcion: a.descripcion,
                cantidad: Number(a.cantidad),
                dimension: a.dimension
            }))
        };

        /* ============================
           TOTAL
        ============================ */

        const totalTexto =
            document.getElementById('resTotal')
                ?.innerText || '';

        let total =
            parseFloat(
                totalTexto.replace(/[^\d.]/g, '')
            );

        if (!Number.isFinite(total) || total <= 0) {
            alert(
                '⚠️ No se pudo obtener el total de la cotización.'
            );
            return;
        }

        if (tipoEntrega === 'ESTACION') {
            total += 3;
        }

        /* ============================
           COORDENADAS DEL MAPA
        ============================ */

        let latitud = null;
        let longitud = null;

        const comboDireccionCoordenadas =
            document.getElementById(
                'comboDireccionesFrecuentes'
            );

        const direccionSeleccionadaCoordenadas =
            comboDireccionCoordenadas?.value || '';

        const usuarioLogueadoCoordenadas =
            typeof idUsuarioLogueado !==
            'undefined' &&
            idUsuarioLogueado !== null;

        const usaDireccionManual =
            tipoEntrega === 'DELIVERY' &&
            (
                !usuarioLogueadoCoordenadas ||
                !direccionSeleccionadaCoordenadas ||
                direccionSeleccionadaCoordenadas ===
                'MANUAL'
            );

        if (
            usaDireccionManual &&
            typeof leafletMarker !== 'undefined' &&
            leafletMarker
        ) {
            const coordenadas =
                leafletMarker.getLatLng();

            if (
                Number.isFinite(coordenadas.lat) &&
                Number.isFinite(coordenadas.lng)
            ) {
                latitud = coordenadas.lat;
                longitud = coordenadas.lng;
            }
        }

        /* ============================
           PAYLOAD FINAL
        ============================ */

        const payload = {
            nombreCliente: nombre,
            telefono,
            correo,
            detalleJson: JSON.stringify(detalle),
            total,
            tipoEntrega,
            direccionEntrega: direccionFinal,
            fechaSolicitada,
            idUsuario:
                typeof idUsuarioLogueado !== 'undefined'
                    ? idUsuarioLogueado
                    : null,

            // Solo serán recibidos si existen en CrearReservaDto.
            latitud,
            longitud
        };

        const modalReservaElement =
            document.getElementById('reservaModal');

        const loadingElement =
            document.getElementById('loadingReservaModal');

        const modalReserva =
            modalReservaElement
                ? bootstrap.Modal.getOrCreateInstance(
                    modalReservaElement
                )
                : null;

        const modalCarga =
            loadingElement
                ? bootstrap.Modal.getOrCreateInstance(
                    loadingElement
                )
                : null;

        try {
            if (btnConfirmar) {
                btnConfirmar.disabled = true;
            }

            if (
                modalReserva &&
                modalReservaElement?.classList.contains('show')
            ) {
                window.preservarDatosReservaAlOcultar =
                    true;

                await new Promise(resolve => {
                    modalReservaElement.addEventListener(
                        'hidden.bs.modal',
                        resolve,
                        { once: true }
                    );

                    modalReserva.hide();
                });
            }

            modalCarga?.show();

            const response = await fetch(
                config.urls.reserva,
                {
                    method: 'POST',
                    headers: {
                        'Content-Type':
                            'application/json',
                        'X-Requested-With':
                            'XMLHttpRequest'
                    },
                    body: JSON.stringify(payload)
                }
            );

            if (!response.ok) {
                const mensajeServidor =
                    await response.text();

                throw new Error(
                    mensajeServidor ||
                    'No se pudo registrar la reserva.'
                );
            }

            /*
             * ReservaController devuelve un RedirectToAction.
             * fetch sigue la redirección y response.url termina
             * apuntando a la confirmación.
             */
            if (response.redirected && response.url) {
                window.location.assign(response.url);
                return;
            }

            const usuarioEstaLogueado =
                typeof idUsuarioLogueado !==
                'undefined' &&
                idUsuarioLogueado !== null;

            window.location.assign(
                usuarioEstaLogueado
                    ? '/Cliente/WebPrincipal'
                    : '/'
            );

        } catch (error) {
            console.error(
                'Error al registrar reserva:',
                error
            );

            modalCarga?.hide();
            modalReserva?.show();

            alert(
                error.message ||
                'No se pudo registrar la reserva. Inténtalo nuevamente.'
            );

        } finally {
            if (btnConfirmar) {
                btnConfirmar.disabled = false;
            }
        }
    }

    /* ============================
       GETTERS
    ============================ */

    function getProductoId() {
        return productoId;
    }

    function getDimension() {
        return dimensionSeleccionada;
    }

    function reset() {
        productoId = null;
        dimensionSeleccionada = null;
        tuboSeleccionado = null; // 👈 AJUSTE: También limpiamos la descripción
        accesorios = [];
    }

    /* 👇 AGREGA ESTO AQUÍ */
    function recargarProductos() {
        cargarProductos();
    }

    // 🚀 AHORA SÍ: LAS FUNCIONES VIVEN ADENTRO DEL MÓDULO 🚀
    function abrirModalCarritoAccesorios() {
        const panel = document.getElementById('panelCarritoAccesorios');
        if (panel) {
            panel.classList.add('active');
            // Detiene el scroll del fondo del body mientras el panel está abierto
            document.body.style.overflow = 'hidden';
        }
    }

    function cerrarPanelCarritoAccesorios() {
        const panel = document.getElementById('panelCarritoAccesorios');
        if (panel) {
            panel.classList.remove('active');
            // Devuelve el comportamiento normal del scroll al body
            document.body.style.overflow = '';
        }
    }


    return {
        init,
        agregarAccesorio,
        quitarAccesorio,
        getAccesorios,
        getProductoId,
        getDimension,
        reset,
        recargarProductos,
        abrirModalCarritoAccesorios,
        cerrarPanelCarritoAccesorios
    };


})();

// ==================================================================
// 🎛️ DESPLAZAMIENTO TÁCTIL Y MOUSE DEL SLIDER GLOBAL (UNIFICADO)
// ==================================================================
window.handleSliderGlobal = function (e, container) {
    const MAX_METROS = 5.95;
    const rect = container.getBoundingClientRect();

    // Captura tanto el click del mouse como el toque del dedo en celulares
    let clientX = e.clientX || (e.touches ? e.touches[0].clientX : 0);
    let widthPercent = Math.max(0, Math.min(100, ((clientX - rect.left) / rect.width) * 100));

    // Conversión matemática exacta a metros totales
    let totalMedida = (widthPercent / 100) * MAX_METROS;

    const metrosInput = document.getElementById("metrosInput");
    const cmInput = document.getElementById("cmInput");

    if (metrosInput && cmInput) {
        let mt = Math.floor(totalMedida);
        let cm = Math.min(99, Math.round((totalMedida % 1) * 100));

        // Candado estricto para que no rompa el límite de 5.95 m
        if (mt === 5 && cm > 95) cm = 95;
        if (mt > 5) { mt = 5; cm = 95; }

        metrosInput.value = mt;
        cmInput.value = cm;

        // 🧠 CONTROL VISUAL EN CALIENTE DIRECTO DESDE EL CORE:
        // Calculamos el total real limpio para pintar los textos en vivo
        let totalReal = mt + (cm / 100);

        const lbl = document.getElementById('lblMetros');
        const visual = document.getElementById('tuboVisual');
        const largoOculto = document.getElementById('largo');

        if (largoOculto) largoOculto.value = totalReal.toFixed(2);
        if (lbl) lbl.innerText = totalReal.toFixed(2);
        if (visual) visual.style.width = (totalReal / MAX_METROS) * 100 + "%";

        // Disparamos el evento nativo para cualquier cálculo extra síncrono del backend
        metrosInput.dispatchEvent(new Event('input'));
    }
};

// Escuchadores de eventos reactivos para el contenedor del riel
document.addEventListener("DOMContentLoaded", () => {
    var sliderContainerGlobal = document.getElementById("progressBarContainer");
    if (sliderContainerGlobal) {
        // Eventos Mouse PC (CORREGIDOS CON WINDOW)
        sliderContainerGlobal.addEventListener("mousedown", function (e) { window.activeDraggingContainer = sliderContainerGlobal; window.handleSliderGlobal(e, sliderContainerGlobal); });
        document.addEventListener("mousemove", (e) => { if (window.activeDraggingContainer === sliderContainerGlobal) window.handleSliderGlobal(e, sliderContainerGlobal); });
        document.addEventListener("mouseup", () => { if (window.activeDraggingContainer === sliderContainerGlobal) window.activeDraggingContainer = null; });

        // Eventos Touch Móvil (CORREGIDOS CON WINDOW)
        sliderContainerGlobal.addEventListener("touchstart", function (e) { e.preventDefault(); window.activeDraggingContainer = sliderContainerGlobal; window.handleSliderGlobal(e, sliderContainerGlobal); }, { passive: false });
        document.addEventListener("touchmove", function (e) { if (window.activeDraggingContainer === sliderContainerGlobal) { e.preventDefault(); window.handleSliderGlobal(e, window.activeDraggingContainer); } }, { passive: false });
        document.addEventListener("touchend", () => { if (window.activeDraggingContainer === sliderContainerGlobal) window.activeDraggingContainer = null; });
    }
});


function abrirModalCarritoAccesorios() {
    const panel = document.getElementById('panelCarritoAccesorios');
    if (panel) {
        panel.classList.add('active');
        // Detiene el scroll del fondo del body mientras el panel está abierto
        document.body.style.overflow = 'hidden';
    }
}

function cerrarPanelCarritoAccesorios() {
    const panel = document.getElementById('panelCarritoAccesorios');
    if (panel) {
        panel.classList.remove('active');
        // Devuelve el comportamiento normal del scroll al body
        document.body.style.overflow = '';
    }
}

// ==================================================================
// 📏 MEDIDOR MAESTRO GLOBAL (Compartido por todos los cotizadores)
// ==================================================================
if (typeof window.estaPresionado === 'undefined') window.estaPresionado = false;
if (typeof window.intervaloMedida === 'undefined') window.intervaloMedida = null;

window.iniciarCambioContinuo = function (e, funcion, valor) {
    // 🔒 Candado estricto: Bloqueo de mousedown si es touch en móvil para evitar doble incremento
    if (e.type === 'mousedown' && 'ontouchstart' in window) return;

    window.detenerCambioContinuo();
    window.estaPresionado = true;

    // Ejecuta UN solo toque instantáneo
    funcion(valor);

    // 📱 UX Móvil: Ya no usamos setInterval automático directo para evitar errores de ráfagas infinitas.
    // Si el cliente quiere ráfaga, tendría que arrastrar el tirador del slider. Los botones son de precisión.
};

window.detenerCambioContinuo = function () {
    window.estaPresionado = false;
    if (window.intervaloMedida) {
        clearInterval(window.intervaloMedida);
        window.intervaloMedida = null;
    }
};

window.cambiarMetro = function (valor) {
    const input = document.getElementById('metrosInput');
    if (!input) return;
    let actual = parseInt(input.value) || 0;
    actual += valor;

    if (actual < 0) actual = 0;
    if (actual > 5) actual = 5;

    input.value = actual;

    const cmInput = document.getElementById('cmInput');
    if (actual === 5 && cmInput && parseInt(cmInput.value) > 95) {
        cmInput.value = 95;
    }

    // 🔥 REFRESCO VISUAL EN CALIENTE PARA LOS BOTONES
    let totalReal = actual + ((cmInput ? parseInt(cmInput.value) : 0) / 100);
    document.getElementById('largo').value = totalReal.toFixed(2);
    document.getElementById('lblMetros').innerText = totalReal.toFixed(2);
    document.getElementById('tuboVisual').style.width = (totalReal / 5.95) * 100 + "%";

    input.dispatchEvent(new Event('input'));
};

window.cambiarCm = function (valor) {
    const input = document.getElementById('cmInput');
    const metrosInput = document.getElementById('metrosInput');
    if (!input) return;

    const metros = metrosInput ? (parseInt(metrosInput.value) || 0) : 0;
    let actual = parseInt(input.value) || 0;
    actual += valor;

    if (actual < 0) actual = 0;

    let maxLocal = (metros === 5) ? 95 : 99;
    if (actual > maxLocal) actual = maxLocal;

    input.value = actual;

    // 🔥 REFRESCO VISUAL EN CALIENTE PARA LOS BOTONES
    let totalReal = metros + (actual / 100);
    document.getElementById('largo').value = totalReal.toFixed(2);
    document.getElementById('lblMetros').innerText = totalReal.toFixed(2);
    document.getElementById('tuboVisual').style.width = (totalReal / 5.95) * 100 + "%";

    input.dispatchEvent(new Event('input'));
};

// ==================================================================
// ⚓ GESTOR DE PASOS Y BARRAS STICKY FLOTANTES GLOBAL
// ==================================================================
window.mostrarSoloPasoGlobal = function (pasoId, listadoPasos = []) {
    // 1. Ocultamos todas las secciones que le pases en el array (ej. paso1, pasoMedida, paso2, resultado)
    if (listadoPasos.length > 0) {
        listadoPasos.forEach(id => {
            const el = document.getElementById(id);
            if (el) el.classList.add('d-none');
        });
    }

    // 2. Mostramos la sección actual de destino
    const pasoActual = document.getElementById(pasoId);
    if (pasoActual) pasoActual.classList.remove('d-none');

    // 3. Mapeamos todas las barras sticky que puedan existir en el HTML
    const barra1 = document.getElementById('barraStickyPaso1');
    const barra2 = document.getElementById('barraStickyMedidas');
    const barra3 = document.getElementById('barraStickyAccesorios');
    const burbuja = document.getElementById('btnCarritoFlotante');

    // Apagamos todas las barras flotantes por defecto para limpiar la pantalla
    if (barra1) barra1.classList.add('d-none');
    if (barra2) barra2.classList.add('d-none');
    if (barra3) barra3.classList.add('d-none');
    if (burbuja) burbuja.classList.add('d-none');

    // 4. Encendido inteligente automatizado según la pantalla activa
    if (pasoId === 'paso1') {
        if (barra1) barra1.classList.remove('d-none');
    }
    else if (pasoId === 'bloqueMedidas' || pasoId === 'pasoMedida') {
        // Soporta tanto 'bloqueMedidas' (Cortinas) como 'pasoMedida' (Pasamanos)
        if (barra2) barra2.classList.remove('d-none');
    }
    else if (pasoId === 'paso2') {
        if (barra3) barra3.classList.remove('d-none'); // Enciende el botón azul final
        if (burbuja) burbuja.classList.remove('d-none'); // Enciende el carrito flotante
    }
    else if (pasoId === 'resultadoHtml' || pasoId === 'resultado') {
        // Si entra al resultado, cerramos de golpe el panel del carrito si quedó abierto
        if (typeof Cotizacion.cerrarPanelCarritoAccesorios === 'function') {
            Cotizacion.cerrarPanelCarritoAccesorios();
        }
    }
};

window.nuevaCotizacion = function () {
    window.location.reload();
};