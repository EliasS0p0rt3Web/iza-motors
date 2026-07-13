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

        const res = await fetch(url);
        const data = await res.json();

        const cont = document.getElementById('contenedorTubos');
        if (!cont) return;

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

        // 🏗️ Maquetación: Imagen más grande a la izquierda, píldoras responsivas a la derecha
        cont.innerHTML = `
    <div class="col-12 col-md-6 text-center border-end-md">
        <div class="p-3 d-flex flex-column align-items-center justify-content-center" style="min-height: 420px;">
            <img id="masterTuboImg" src="${productoMaestro.imagen ?? '/img/home/placeholder.png'}" 
            class="img-fluid mb-4 custom-master-img">
            
            <h3 id="masterTuboTitulo" class="fw-extrabold text-dark text-uppercase mb-4" style="font-size: 1.35rem; letter-spacing: 0.5px;">
                ${productoMaestro.descripcion}
            </h3>
            
            <button type="button" class="btn btn-success px-5 py-2.5 fw-bold btnConfirmarTuboMaster" 
                    style="font-size: 1.05rem; background-color: #16a34a; border: none; border-radius: 50px; box-shadow: 0 4px 12px rgba(22, 163, 74, 0.2);">
                 Seleccionar
            </button>
        </div>
    </div>

    <div class="col-12 col-md-6 d-flex align-items-center">
        <div class="p-3 w-100 container-pills-wrapper">
            <span class="text-secondary fw-bold small text-uppercase d-block mb-3" style="letter-spacing: 0.5px;">
                Selecciona la Dimensión / Diámetro:
            </span>
            <div class="d-flex flex-column flex-md-row flex-md-wrap gap-2 text-center text-md-start" id="contenedorDimensionesPills">
                </div>
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

            // Al hacer clic en una píldora SOLO cambiamos la vista visual
            btn.addEventListener('click', function () {
                contenedorPills.querySelectorAll('.btn-dimension-pill').forEach(b => b.classList.remove('active'));
                this.classList.add('active');

                // Guardamos la referencia de los datos actuales del botón
                varianteTemporalSeleccionada = {
                    id: this.dataset.id,
                    dimension: this.dataset.dimension,
                    descripcion: this.dataset.descripcion
                };

                // Efecto óptico en la imagen maestra
                const masterImg = document.getElementById('masterTuboImg');

                // 🗑️ SE ELIMINARON LAS LÍNEAS QUE BUSCABAN EL BADGE AZUL AQUÍ

                if (masterImg) {
                    masterImg.style.transform = 'scale(0.92)';
                    setTimeout(() => {
                        masterImg.src = this.dataset.imagen;
                        masterImg.style.transform = 'scale(1)';
                    }, 100);
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
        return document.querySelector('input[name="tipoVenta"]:checked')?.value;
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

        const cont = document.getElementById('listaAccesorios');
        if (!cont) return;

        if (!accesorios.length) {
            cont.innerText = 'Ninguno';
            return;
        }

        cont.innerHTML =
            '<ul>' +
            accesorios.map((a, i) => `
                <li>
                    ${a.descripcion} x ${a.cantidad}
                    <button onclick="Cotizacion.quitarAccesorio(${i})">✕</button>
                </li>
            `).join('') +
            '</ul>';
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

        // 🔥 Payload base (el que usa cortina)
        const basePayload = {
            tuboProductoId: productoId,
            tipoVenta: getTipoVenta(),
            largoMetros: parseFloat(document.getElementById('largo')?.value || 0),
            cantidadPiezas: parseInt(document.getElementById('piezas')?.value || 1),
            accesorios: accesorios.map(a => ({
                productoId: a.productoId,
                cantidad: a.cantidad
            }))
        };

        // 🔥 Si el módulo define un mapper, lo usamos
        const payload = typeof config.mapPayload === "function"
            ? config.mapPayload(basePayload)
            : basePayload;

        const res = await fetch(config.urls.calcular, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const r = await res.json();

        document.dispatchEvent(new CustomEvent('mostrarResultado', {
            detail: r
        }));
    }

    /* ============================
       RESERVA
    ============================ */

    function initReserva() {

        document.getElementById('btnReservar')
            ?.addEventListener('click', () => {
                new bootstrap.Modal(
                    document.getElementById('reservaModal')
                ).show();
            });

        // 👇 Control de visibilidad: Recojo vs Delivery vs Tren
        document.getElementById('clienteEntrega')
            ?.addEventListener('change', function () {
                const grupoDireccion = document.getElementById('grupoDireccion');
                const grupoEstaciones = document.getElementById('grupoEstaciones');

                if (!grupoDireccion || !grupoEstaciones) return;

                // Ocultamos todo primero
                grupoDireccion.classList.add('d-none');
                grupoEstaciones.classList.add('d-none');

                // Mostramos según la elección
                if (this.value === 'DELIVERY') {
                    grupoDireccion.classList.remove('d-none');
                } else if (this.value === 'ESTACION') {
                    grupoEstaciones.classList.remove('d-none');
                }
            });

        document.getElementById('btnConfirmarReserva')
            ?.addEventListener('click', confirmarReserva);
    }

    async function confirmarReserva() {
        if (!config.urls?.reserva) return;

        const nombre = document.getElementById('clienteNombre')?.value.trim();
        const telefono = document.getElementById('clienteTelefono')?.value.trim();
        const correo = document.getElementById('clienteCorreo')?.value.trim();
        const tipoEntrega = document.getElementById('clienteEntrega')?.value;
        const direccion = document.getElementById('clienteDireccion')?.value.trim();
        const estacion = document.getElementById('estacionTren')?.value;

        if (!nombre || !telefono) {
            alert("Completa tus datos");
            return;
        }

        // 🚀 1. GESTIÓN DE MODALES
        const modalElement = document.getElementById('reservaModal');
        const modalRegistro = bootstrap.Modal.getInstance(modalElement);
        const modalCarga = new bootstrap.Modal(document.getElementById('loadingModal'));

        if (modalRegistro) modalRegistro.hide();
        modalCarga.show();

        // 💰 2. LÓGICA DE PRECIO Y DIRECCIÓN
        const totalTexto = document.getElementById('resTotal')?.innerText;
        let total = parseFloat(totalTexto?.replace(/[^\d.]/g, '') || 0);
        let direccionFinal = "";

        if (tipoEntrega === "ESTACION") {
            total += 3.00; // Sumamos el delivery fijo del tren
            direccionFinal = "ENTREGA EN ESTACIÓN: " + estacion;
        } else if (tipoEntrega === "DELIVERY") {
            direccionFinal = direccion;
        } else {
            direccionFinal = "RECOJO EN TIENDA (CANTAGALLO)";
        }

        const modalidad = getTipoVenta();

        const detalle = {
            productoId: productoId,
            tubo: tuboSeleccionado,
            dimension: dimensionSeleccionada,
            tipoVenta: modalidad,
            // 🚀 SEPARACIÓN DE PODERES:
            Metros: document.getElementById('largo')?.value || "1.00",
            CantidadPiezas: document.getElementById('piezas')?.value || "1",
            accesorios: accesorios
        };

        const payload = {
            nombreCliente: nombre,
            telefono: telefono,
            correo: correo,
            detalleJson: JSON.stringify(detalle),
            total: total.toFixed(2), // Enviamos el total actualizado con los S/ 3
            tipoEntrega: tipoEntrega,
            direccionEntrega: direccionFinal,
            fechaSolicitada: new Date().toISOString()
        };

        try {
            const res = await fetch(config.urls.reserva, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok || res.redirected) {
                window.location.href = res.url;
            }
        } catch (err) {
            console.warn("Latencia en correos detectada. Redirigiendo en breve...");
            // El modal de carga se queda ahí hasta que el servidor responda
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



    return {
        init,
        agregarAccesorio,
        quitarAccesorio,
        getAccesorios,
        getProductoId,
        getDimension,
        reset,
        recargarProductos 
    };

})();