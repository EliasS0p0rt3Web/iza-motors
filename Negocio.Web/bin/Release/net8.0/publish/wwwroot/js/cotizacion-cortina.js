let tipoInstalacion = null;
let tipoUso = null;
let areaSeleccionada = null;
let estaPresionado = false;
document.addEventListener('DOMContentLoaded', () => {
    mostrarSoloPaso('paso1');

    new bootstrap.Modal(
        document.getElementById('materialModal')
    ).show();
});

document.addEventListener('productoSeleccionado', (e) => {

    // 1. Ocultamos el botón de material para limpiar la interfaz
    const contenedorMat = document.getElementById('contenedorBotonMaterial');
    if (contenedorMat) {
        contenedorMat.classList.add('d-none');
    }

    // 2. Gestión de pasos y UI
    mostrarSoloPaso('bloqueMedidas');
    irPaso(2); // 🔥 CAMBIO CLAVE: Ahora Medidas es el paso 2 en el wizard de 4

    // 3. Lógica del Hint (Globo de ayuda)
    const hint = document.getElementById('hintUsuario');
    const destino = document.getElementById('bloqueMedidas');

    if (hint && destino) {
        destino.style.position = "relative";
        destino.appendChild(hint);
        hint.classList.remove('d-none');
        hint.style.display = "block";

        // Ajuste fino para que flote sobre el botón siguiente
        hint.style.bottom = "110px";
    }

    // 4. Función para matar el aviso al interactuar (UX limpia)
    const eliminarAviso = () => {
        hint?.classList.add('d-none');
        document.removeEventListener('click', eliminarAviso);
        document.getElementById('metrosInput')?.removeEventListener('focus', eliminarAviso);
        document.getElementById('cmInput')?.removeEventListener('focus', eliminarAviso);
    };

    // El aviso se va si hacen click o entran a los inputs
    setTimeout(() => {
        document.addEventListener('click', eliminarAviso);
        document.getElementById('metrosInput')?.addEventListener('focus', eliminarAviso);
        document.getElementById('cmInput')?.addEventListener('focus', eliminarAviso);
    }, 500);
});


// --- VALIDACIÓN DE ESCRITURA PARA CENTÍMETROS ---
document.getElementById('cmInput')?.addEventListener('input', function () {
    const metros = parseInt(document.getElementById('metrosInput').value) || 0;
    let valor = parseInt(this.value) || 0;

    if (valor < 0) this.value = 0;

    let tope = (metros === 5) ? 95 : 99;
    if (valor > tope) this.value = tope;

    // Si borra el número, dejarlo en 0 para evitar errores
    if (this.value === "") this.value = 0;
});

document.getElementById('btnPaso2')?.addEventListener('click', () => {
    if (!Cotizacion.getProductoId()) {
        alert('Selecciona un tubo primero');
        return;
    }
    new bootstrap.Modal(document.getElementById('configModal')).show();
});

document.getElementById('btnConfirmConfig')?.addEventListener('click', async () => {

    tipoInstalacion = document.getElementById('tipoInstalacion').value;
    tipoUso = document.getElementById('tipoUso').value;

    // Cerramos el modal de configuración
    bootstrap.Modal.getInstance(document.getElementById('configModal'))?.hide();

    // 1. Mostramos la sección de accesorios
    mostrarSoloPaso('paso2');

    // 2. 🔥 CAMBIO CLAVE: Ahora Accesorios es el paso 3
    irPaso(3);

    // 3. Cargamos los accesorios desde la base de datos
    await cargarAccesorios();

    // 4. Scroll suave para que el usuario vea el inicio de los accesorios
    setTimeout(() => {
        const destino = document.getElementById('paso2');
        if (destino) {
            const top = destino.getBoundingClientRect().top + window.scrollY - 120;
            window.scrollTo({ top, behavior: 'smooth' });
        }
    }, 150);
});
async function cargarAccesorios() {
    const diametro = Cotizacion.getDimension();
    if (!diametro) return;

    const res = await fetch(
        `${urlGetAccesorios}?diametro=${encodeURIComponent(diametro)}&instalacion=${tipoInstalacion}&uso=${tipoUso}`
    );

    const data = await res.json();
    const cont = document.getElementById('contenedorAccesorios');
    cont.innerHTML = '';

    data.forEach(a => {
        cont.innerHTML += `
        <div class="col-6 col-md-4 col-lg-3">
            <div class="tubo-card accesorio-card"
                 data-id="${a.idProducto}"
                 data-desc="${a.descripcion}"
                 data-dim="${a.dimension}"> 

                <img src="${a.imagen ?? '/img/home/placeholder.jpg'}">
                <b>${a.descripcion}</b>
                <small class="d-block text-muted">${a.dimension}</small>
                <div class="precio mb-3">S/ ${a.precio}</div>

                <!-- CONTROLES CIRCULARES TIPO MEDIDOR -->
                <div class="d-flex justify-content-center align-items-center gap-2 mb-3">
                    <button type="button" class="medida-control-btn btn-minus"
                            onclick="UI.cambiarCantidadCard(this,-1)">−</button>

                    <input type="number" class="medida-input-clean cantidad-acc"
                           value="1" min="1" readonly>

                    <button type="button" class="medida-control-btn btn-plus"
                            onclick="UI.cambiarCantidadCard(this,1)">+</button>
                </div>

                <button class="btn btn-success w-100"
                        onclick="agregarAccesorioDesdeCard(this)">
                    Agregar
                </button>
            </div>
        </div>
    `;
    });
}

function agregarAccesorioDesdeCard(btn) {
    const card = btn.closest('.accesorio-card');
    const productoId = card.dataset.id;
    const descripcion = card.dataset.desc;
    const dimension = card.dataset.dim;
    const cantidad = parseInt(card.querySelector('.cantidad-acc').value) || 1;

    UI.animacionPremium(card);
    Cotizacion.agregarAccesorio(productoId, descripcion, cantidad, dimension);

    card.querySelector('.cantidad-acc').value = 1;
}

// 1. Al seleccionar material, entramos al PASO 1 (Productos)
function seleccionarMaterial(material) {
    areaSeleccionada = material;

    const txtMaterial = document.getElementById('txtMaterialDinamico');
    if (txtMaterial) {
        const nombreFormateado = material.charAt(0).toUpperCase() + material.slice(1).toLowerCase();
        txtMaterial.innerText = nombreFormateado;
    }

    // Cerramos modal y marcamos el paso 1 del wizard
    bootstrap.Modal.getInstance(document.getElementById('materialModal'))?.hide();
    mostrarSoloPaso('paso1');
    irPaso(1); // Ahora el material nos lleva al inicio del wizard (Productos)

    Cotizacion.reset();
    Cotizacion.recargarProductos();
}
function abrirModalMaterial() {
    new bootstrap.Modal(
        document.getElementById('materialModal')
    ).show();
}

function mostrarSoloPaso(pasoId) {
    ['paso1', 'bloqueMedidas', 'paso2', 'resultadoHtml']
        .forEach(id => document.getElementById(id)?.classList.add('d-none'));

    document.getElementById(pasoId)?.classList.remove('d-none');
}

function irPaso(numero) {
    document.querySelectorAll('.wizard-step').forEach(step => {
        const stepNum = parseInt(step.dataset.step);
        step.classList.remove('active', 'completed');
        if (stepNum < numero) step.classList.add('completed');
        if (stepNum === numero) step.classList.add('active');
    });
}

Cotizacion.init({
    maxMetros: 5.95,
    urls: {
        getProductos: urlGetTubos,
        calcular: urlCalcular,
        reserva: urlReservaCrear
    },
    getMaterial: () => areaSeleccionada
});

let intervaloMedida;

function iniciarCambioContinuo(e, funcion, valor) {
    // 🔥 BLOQUEO CRÍTICO: 
    // Si es un evento de mouse pero el dispositivo es táctil, lo ignoramos
    // para que no se sume doble (+2).
    if (e.type === 'mousedown' && 'ontouchstart' in window) return;

    detenerCambioContinuo();
    estaPresionado = true;

    // Ejecuta el primer cambio
    funcion(valor);

    // Configura la repetición (150ms es perfecto para móvil)
    intervaloMedida = setInterval(() => {
        if (estaPresionado) funcion(valor);
    }, 150);
}

function detenerCambioContinuo() {
    estaPresionado = false;
    if (intervaloMedida) {
        clearInterval(intervaloMedida);
        intervaloMedida = null;
    }
}

// --- ACTUALIZACIÓN DE LAS FUNCIONES DE CAMBIO ---

function cambiarMetro(valor) {
    const input = document.getElementById('metrosInput');
    let actual = parseInt(input.value) || 0;
    actual += valor;

    if (actual < 0) actual = 0;
    if (actual > 5) actual = 5; // El máximo sigue siendo 5.95

    input.value = actual;

    // Si metros sube a 5 y cm ya era > 95, bajamos cm a 95
    const cmInput = document.getElementById('cmInput');
    if (actual === 5 && parseInt(cmInput.value) > 95) {
        cmInput.value = 95;
    }

    input.dispatchEvent(new Event('input'));
}

// En la función cambiarCm(valor)
function cambiarCm(valor) {
    const input = document.getElementById('cmInput');
    const metros = parseInt(document.getElementById('metrosInput').value) || 0;
    let actual = parseInt(input.value) || 0;
    actual += valor;

    if (actual < 0) actual = 0;

    // Lógica estricta de 95 para 5 metros
    let maxLocal = (metros === 5) ? 95 : 99;
    if (actual > maxLocal) actual = maxLocal;

    input.value = actual;
    input.dispatchEvent(new Event('input'));
}

function cambiarVarilla(valor) {

    const input = document.getElementById('piezas');
    if (!input) return;

    let actual = parseInt(input.value) || 1;
    actual += valor;

    if (actual < 1) actual = 1;

    input.value = actual;
}

// ==========================================
// 🚀 LÓGICA PREMIUM: CÁLCULO EN VIVO Y TICKET
// ==========================================
let precioBaseUnidad = 0;

document.addEventListener('mostrarResultado', (e) => {
    const r = e.detail;

    // 1. Guardamos el precio de 1 unidad para multiplicar localmente
    precioBaseUnidad = parseFloat(r.total) || 0;

    // 2. Reseteamos el contador visual a 1
    const inputPiezas = document.getElementById('piezas');
    if (inputPiezas) inputPiezas.value = 1;

    // 3. Renderizamos datos básicos del ticket
    document.getElementById('resTuboDesc').innerText = r.tubo ?? '';
    document.getElementById('resTuboDim').innerText = Cotizacion.getDimension() ?? '';
    const imgSeleccionada = document.querySelector('.tubo-card.active img');
    document.getElementById('resTuboImg').src = imgSeleccionada?.src ?? '/img/home/placeholder.jpg';

    const largo = parseFloat(document.getElementById('largo')?.value || 1).toFixed(2);
    document.getElementById('resResumenTubo').innerHTML = `
        <span class="text-white-50">Corte:</span> ${largo} m<br>
        <b class="text-success fs-5 d-block mt-1">S/ ${r.subtotalTubo}</b>
    `;

    // 4. Renderizamos accesorios (formato oscuro)
    const contAcc = document.getElementById('resAccesorios');
    contAcc.innerHTML = '';
    if (r.accesorios && r.accesorios.length > 0) {
        r.accesorios.forEach(a => {
            contAcc.innerHTML += `
                <div class="d-flex justify-content-between align-items-center p-3 rounded-3 mb-2" style="background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.05);">
                    <div>
                        <strong class="text-white text-uppercase" style="font-size: 0.85rem;">${a.descripcion}</strong><br>
                        <small class="text-white-50" style="font-size: 0.75rem;">Cant: ${a.cantidad}</small>
                    </div>
                    <span class="text-success fw-bold">S/ ${a.subtotal}</span>
                </div>`;
        });
    } else {
        contAcc.innerHTML = '<div class="text-center text-white-50 py-2 small">Sin accesorios adicionales</div>';
    }

    // 5. Mostrar ticket y ocultar el resto
    document.getElementById('resTotal').innerText = `S/ ${precioBaseUnidad.toFixed(2)}`;
    document.getElementById('zonaCotizacion')?.classList.add('d-none');
    mostrarSoloPaso('resultadoHtml');
    irPaso(4);

    document.getElementById('btnReservar')?.classList.remove('d-none');
    document.getElementById('resultadoHtml')?.scrollIntoView({ behavior: 'smooth' });
});

// FUNCIÓN MAESTRA DE MULTIPLICACIÓN (Copiada de Baños)
function cambiarPiezasTicket(delta) {
    const input = document.getElementById('piezas');
    const displayTotal = document.getElementById('resTotal');
    if (!input || !displayTotal) return;

    let q = parseInt(input.value) || 1;
    q += delta;
    if (q < 1) q = 1;
    input.value = q;

    // Cálculo instantáneo local
    const nuevoTotal = (precioBaseUnidad * q).toFixed(2);
    displayTotal.innerText = `S/ ${nuevoTotal}`;

    // Efecto de pulso visual
    displayTotal.style.transition = "transform 0.1s ease";
    displayTotal.style.transform = "scale(1.1)";
    setTimeout(() => { displayTotal.style.transform = "scale(1)"; }, 100);
}
function abrirModalSalir() {
    const modal = new bootstrap.Modal(
        document.getElementById('modalSalir')
    );
    modal.show();
}

// ================================
// NUEVA COTIZACIÓN GLOBAL
// ================================
window.nuevaCotizacion = function () {
    location.reload();
};

