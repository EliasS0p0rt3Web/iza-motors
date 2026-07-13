let tipoInstalacion = null;
let tipoUso = null;
let areaSeleccionada = null;
let estaPresionado = false;

document.addEventListener('DOMContentLoaded', () => {
    mostrarSoloPaso('paso1');

    const materialModalElement =
        document.getElementById('materialModal');

    if (materialModalElement) {
        bootstrap.Modal
            .getOrCreateInstance(materialModalElement)
            .show();
    }
});

document.addEventListener('productoSeleccionado', (e) => {
    // 1. Ocultamos el botón de material para limpiar la interfaz
    const contenedorMat =
        document.getElementById('contenedorBotonMaterial');

    if (contenedorMat) {
        contenedorMat.classList.add('d-none');
    }

    // El loader de cálculo será controlado solamente por cotizacion-core.js
    const destino =
        document.getElementById('bloqueMedidas');

    const hint =
        document.getElementById('hintUsuario');

    // 2. Gestión de pasos y UI instantánea
    mostrarSoloPaso('bloqueMedidas');
    irPaso(2);

    // 3. Lógica del Hint
    if (hint && destino) {
        destino.style.position = "relative";
        destino.appendChild(hint);
        hint.classList.remove('d-none');
        hint.style.display = "block";
        hint.style.bottom = "110px";
    }

    // 4. Eliminar el aviso cuando el usuario interactúe
    const eliminarAviso = () => {
        hint?.classList.add('d-none');

        document.removeEventListener(
            'click',
            eliminarAviso
        );

        document
            .getElementById('metrosInput')
            ?.removeEventListener(
                'focus',
                eliminarAviso
            );

        document
            .getElementById('cmInput')
            ?.removeEventListener(
                'focus',
                eliminarAviso
            );
    };

    setTimeout(() => {
        document.addEventListener(
            'click',
            eliminarAviso
        );

        document
            .getElementById('metrosInput')
            ?.addEventListener(
                'focus',
                eliminarAviso
            );

        document
            .getElementById('cmInput')
            ?.addEventListener(
                'focus',
                eliminarAviso
            );
    }, 500);
});

// --- VALIDACIÓN DE ESCRITURA PARA CENTÍMETROS ---
document
    .getElementById('cmInput')
    ?.addEventListener('input', function () {
        const metrosInput =
            document.getElementById('metrosInput');

        const metros =
            parseInt(metrosInput?.value, 10) || 0;

        let valor =
            parseInt(this.value, 10) || 0;

        if (valor < 0) {
            this.value = 0;
        }

        const tope =
            metros === 5 ? 95 : 99;

        if (valor > tope) {
            this.value = tope;
        }

        if (this.value === "") {
            this.value = 0;
        }
    });

document
    .getElementById('btnPaso2')
    ?.addEventListener('click', () => {
        if (!Cotizacion.getProductoId()) {
            alert('Selecciona un tubo primero');
            return;
        }

        const configModalElement =
            document.getElementById(
                'configModal'
            );

        if (configModalElement) {
            bootstrap.Modal
                .getOrCreateInstance(
                    configModalElement
                )
                .show();
        }
    });

document
    .getElementById('btnConfirmConfig')
    ?.addEventListener(
        'click',
        async function () {
            if (this.disabled) return;

            this.disabled = true;

            try {
                const tipoInstalacionElement =
                    document.getElementById(
                        'tipoInstalacion'
                    );

                const tipoUsoElement =
                    document.getElementById(
                        'tipoUso'
                    );

                if (
                    !tipoInstalacionElement ||
                    !tipoUsoElement
                ) {
                    throw new Error(
                        'No se pudo leer la configuración de instalación.'
                    );
                }

                tipoInstalacion =
                    tipoInstalacionElement.value;

                tipoUso =
                    tipoUsoElement.value;

                // Cerramos el modal de configuración
                const configModalElement =
                    document.getElementById(
                        'configModal'
                    );

                if (configModalElement) {
                    bootstrap.Modal
                        .getOrCreateInstance(
                            configModalElement
                        )
                        .hide();
                }

                // Mostramos accesorios
                mostrarSoloPaso('paso2');
                irPaso(3);

                // Cargamos accesorios desde el servidor
                await cargarAccesorios();

                // Scroll suave hacia accesorios
                setTimeout(() => {
                    const destino =
                        document.getElementById(
                            'paso2'
                        );

                    if (destino) {
                        const top =
                            destino
                                .getBoundingClientRect()
                                .top +
                            window.scrollY -
                            120;

                        window.scrollTo({
                            top,
                            behavior: 'smooth'
                        });
                    }
                }, 150);

            } catch (error) {
                console.error(
                    'Error al preparar los accesorios:',
                    error
                );

                alert(
                    error.message ||
                    'No se pudo preparar la configuración de accesorios.'
                );

            } finally {
                this.disabled = false;
            }
        }
    );

async function cargarAccesorios() {
    const diametro =
        Cotizacion.getDimension();

    const cont =
        document.getElementById(
            'contenedorAccesorios'
        );

    if (!diametro || !cont) return;

    cont.innerHTML = `
        <div class="col-12 text-center py-4">
            <div class="spinner-border text-success mb-2"
                 role="status">
            </div>

            <div class="text-muted small">
                Cargando accesorios...
            </div>
        </div>
    `;

    try {
        const params =
            new URLSearchParams({
                diametro,
                instalacion:
                    tipoInstalacion || '',
                uso:
                    tipoUso || ''
            });

        const res = await fetch(
            `${urlGetAccesorios}?${params.toString()}`
        );

        if (!res.ok) {
            throw new Error(
                `No se pudieron cargar los accesorios. HTTP ${res.status}`
            );
        }

        const data =
            await res.json();

        if (!Array.isArray(data)) {
            throw new Error(
                'La respuesta de accesorios no tiene un formato válido.'
            );
        }

        cont.innerHTML = '';

        if (data.length === 0) {
            cont.innerHTML = `
                <div class="col-12 text-center text-muted py-4">
                    No hay accesorios disponibles para esta configuración.
                </div>
            `;

            return;
        }

        data.forEach(a => {
            cont.innerHTML += `
                <div class="col-6 col-md-4 col-lg-3">
                    <div class="tubo-card accesorio-card"
                         data-id="${a.idProducto}"
                         data-desc="${a.descripcion}"
                         data-dim="${a.dimension}">

                        <img src="${a.imagen ?? '/img/home/placeholder.jpg'}">

                        <b>${a.descripcion}</b>

                        <small class="d-block text-muted">
                            ${a.dimension}
                        </small>

                        <div class="precio mb-3">
                            S/ ${a.precio}
                        </div>

                        <div class="d-flex justify-content-center align-items-center gap-2 mb-3">
                            <button type="button"
                                    class="medida-control-btn btn-minus"
                                    onclick="UI.cambiarCantidadCard(this,-1)">
                                −
                            </button>

                            <input type="number"
                                   class="medida-input-clean cantidad-acc"
                                   value="1"
                                   min="1"
                                   readonly>

                            <button type="button"
                                    class="medida-control-btn btn-plus"
                                    onclick="UI.cambiarCantidadCard(this,1)">
                                +
                            </button>
                        </div>

                        <button type="button"
                                class="btn btn-success w-100"
                                onclick="agregarAccesorioDesdeCard(this)">
                            Agregar
                        </button>
                    </div>
                </div>
            `;
        });

    } catch (error) {
        console.error(
            'Error al cargar accesorios:',
            error
        );

        cont.innerHTML = `
            <div class="col-12 text-center text-danger py-4">
                No se pudieron cargar los accesorios.
                Intenta nuevamente.
            </div>
        `;
    }
}

function agregarAccesorioDesdeCard(btn) {
    const card =
        btn.closest('.accesorio-card');

    if (!card) return;

    const cantidadInput =
        card.querySelector('.cantidad-acc');

    if (!cantidadInput) return;

    const productoId =
        card.dataset.id;

    const descripcion =
        card.dataset.desc;

    const dimension =
        card.dataset.dim;

    const cantidad =
        parseInt(
            cantidadInput.value,
            10
        ) || 1;

    // Efecto visual hacia el carrito
    const burbujaCarrito =
        document.getElementById(
            'btnCarritoFlotante'
        );

    if (
        burbujaCarrito &&
        !burbujaCarrito.classList.contains(
            'd-none'
        )
    ) {
        const rectCard =
            card.getBoundingClientRect();

        const rectCar =
            burbujaCarrito
                .getBoundingClientRect();

        const bolitaVoladora =
            document.createElement('div');

        bolitaVoladora.className =
            'tarjeta-convertida-bolita';

        bolitaVoladora.style.top =
            rectCard.top + 'px';

        bolitaVoladora.style.left =
            rectCard.left + 'px';

        bolitaVoladora.style.width =
            rectCard.width + 'px';

        bolitaVoladora.style.height =
            rectCard.height + 'px';

        document.body.appendChild(
            bolitaVoladora
        );

        requestAnimationFrame(() => {
            bolitaVoladora.style.top =
                rectCar.top + 20 + 'px';

            bolitaVoladora.style.left =
                rectCar.left + 20 + 'px';

            bolitaVoladora.style.width =
                '24px';

            bolitaVoladora.style.height =
                '24px';

            bolitaVoladora.style.borderRadius =
                '50%';

            bolitaVoladora.style.transform =
                'scale(0.4) rotate(180deg)';
        });

        setTimeout(() => {
            bolitaVoladora.remove();
        }, 650);

    } else {
        if (
            typeof UI !== 'undefined' &&
            typeof UI.animacionPremium ===
            'function'
        ) {
            UI.animacionPremium(card);
        }
    }

    Cotizacion.agregarAccesorio(
        productoId,
        descripcion,
        cantidad,
        dimension
    );

    cantidadInput.value = 1;
}

// Al seleccionar material, entramos al paso de productos
function seleccionarMaterial(material) {
    areaSeleccionada = material;

    const txtMaterial =
        document.getElementById(
            'lblMaterial'
        );

    if (txtMaterial) {
        const nombreFormateado =
            material.charAt(0).toUpperCase() +
            material
                .slice(1)
                .toLowerCase();

        txtMaterial.innerText =
            nombreFormateado;
    }

    const materialModalElement =
        document.getElementById(
            'materialModal'
        );

    if (materialModalElement) {
        bootstrap.Modal
            .getOrCreateInstance(
                materialModalElement
            )
            .hide();
    }

    mostrarSoloPaso('paso1');
    irPaso(1);

    Cotizacion.reset();
    Cotizacion.recargarProductos();
}

function abrirModalMaterial() {
    const materialModalElement =
        document.getElementById(
            'materialModal'
        );

    if (materialModalElement) {
        bootstrap.Modal
            .getOrCreateInstance(
                materialModalElement
            )
            .show();
    }
}

function mostrarSoloPaso(pasoId) {
    window.mostrarSoloPasoGlobal(
        pasoId,
        [
            'paso1',
            'bloqueMedidas',
            'paso2',
            'resultadoHtml'
        ]
    );
}

function irPaso(numero) {
    document
        .querySelectorAll('.wizard-step')
        .forEach(step => {
            const stepNum =
                parseInt(
                    step.dataset.step,
                    10
                );

            step.classList.remove(
                'active',
                'completed'
            );

            if (stepNum < numero) {
                step.classList.add(
                    'completed'
                );
            }

            if (stepNum === numero) {
                step.classList.add(
                    'active'
                );
            }
        });
}

Cotizacion.init({
    maxMetros: 5.95,

    urls: {
        getProductos:
            urlGetTubos,

        calcular:
            urlCalcular,

        reserva:
            urlReservaCrear
    },

    getMaterial: () =>
        areaSeleccionada
});

let intervaloMedida;

function cambiarVarilla(valor) {
    const input =
        document.getElementById(
            'piezas'
        );

    if (!input) return;

    let actual =
        parseInt(
            input.value,
            10
        ) || 1;

    actual += valor;

    if (actual < 1) {
        actual = 1;
    }

    input.value = actual;
}

// ==========================================
// LÓGICA DE CÁLCULO EN VIVO Y TICKET
// El loader es controlado solamente por cotizacion-core.js
// ==========================================

let precioBaseUnidad = 0;

window.cambiarPiezasTicket =
    function (valor) {
        const input =
            document.getElementById(
                'piezas'
            );

        const totalElement =
            document.getElementById(
                'resTotal'
            );

        if (
            !input ||
            !totalElement
        ) {
            return;
        }

        let cantidad =
            parseInt(
                input.value,
                10
            ) || 1;

        cantidad += valor;

        if (cantidad < 1) {
            cantidad = 1;
        }

        input.value = cantidad;

        const totalFinal =
            precioBaseUnidad *
            cantidad;

        totalElement.innerText =
            `S/ ${totalFinal.toFixed(2)}`;
    };

document.addEventListener(
    'mostrarResultado',
    e => {
        const r = e.detail;

        const totalResultado =
            Number(r?.total);

        if (
            !r ||
            typeof r !== 'object' ||
            !Number.isFinite(
                totalResultado
            ) ||
            totalResultado <= 0
        ) {
            console.error(
                'Respuesta de cálculo inválida:',
                r
            );

            alert(
                'La respuesta del cálculo no contiene un total válido.'
            );

            return;
        }

        const accesoriosResultado =
            Array.isArray(
                r.accesorios
            )
                ? r.accesorios
                : [];

        // Precio de una unidad
        precioBaseUnidad =
            totalResultado;

        // Reiniciar cantidad visual
        const inputPiezas =
            document.getElementById(
                'piezas'
            );

        if (inputPiezas) {
            inputPiezas.value = 1;
        }

        // Datos del tubo
        const resTuboDesc =
            document.getElementById(
                'resTuboDesc'
            );

        const resTuboDim =
            document.getElementById(
                'resTuboDim'
            );

        const resTuboImg =
            document.getElementById(
                'resTuboImg'
            );

        const resResumenTubo =
            document.getElementById(
                'resResumenTubo'
            );

        if (resTuboDesc) {
            resTuboDesc.innerText =
                r.tubo ?? '';
        }

        if (resTuboDim) {
            resTuboDim.innerText =
                Cotizacion.getDimension() ??
                '';
        }

        const imgSeleccionada =
            document.getElementById(
                'masterTuboImg'
            );

        if (resTuboImg) {
            resTuboImg.src =
                imgSeleccionada?.src ||
                '/img/home/placeholder.jpg';
        }

        const largo =
            parseFloat(
                document
                    .getElementById(
                        'largo'
                    )
                    ?.value || 1
            ).toFixed(2);

        if (resResumenTubo) {
            resResumenTubo.innerHTML = `
                <span class="text-white-50">
                    Corte:
                </span>
                ${largo} m
                <br>
                <b class="text-success fs-5 d-block mt-1">
                    S/ ${r.subtotalTubo ?? '0.00'}
                </b>
            `;
        }

        // Accesorios
        const contAcc =
            document.getElementById(
                'resAccesorios'
            );

        if (contAcc) {
            contAcc.innerHTML = '';

            if (
                accesoriosResultado.length >
                0
            ) {
                accesoriosResultado
                    .forEach(a => {
                        contAcc.innerHTML += `
                            <div class="d-flex justify-content-between align-items-center p-3 rounded-3 mb-2"
                                 style="background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.05);">

                                <div>
                                    <strong class="text-white text-uppercase"
                                            style="font-size: 0.85rem;">
                                        ${a.descripcion}
                                    </strong>

                                    <br>

                                    <small class="text-white-50"
                                           style="font-size: 0.75rem;">
                                        Cant: ${a.cantidad}
                                    </small>
                                </div>

                                <span class="text-success fw-bold">
                                    S/ ${a.subtotal}
                                </span>
                            </div>
                        `;
                    });

            } else {
                contAcc.innerHTML = `
                    <div class="text-center text-white-50 py-2 small">
                        Sin accesorios adicionales
                    </div>
                `;
            }
        }

        // Mostrar ticket
        const ticket =
            document.getElementById(
                'resultadoHtml'
            );

        const displayTotal =
            document.getElementById(
                'resTotal'
            );

        document
            .getElementById(
                'zonaCotizacion'
            )
            ?.classList.add(
                'd-none'
            );

        if (displayTotal) {
            displayTotal.innerText =
                `S/ ${precioBaseUnidad.toFixed(2)}`;
        }

        if (ticket) {
            ticket.classList.remove(
                'd-none'
            );

            ticket.style.transform =
                'scale(0.8)';

            ticket.style.opacity =
                '0';

            ticket.style.transition =
                'none';

            requestAnimationFrame(() => {
                ticket.style.transition =
                    'transform 0.4s cubic-bezier(0.34, 1.56, 0.64, 1), opacity 0.4s ease';

                ticket.style.transform =
                    'scale(1.03)';

                ticket.style.opacity =
                    '1';

                setTimeout(() => {
                    ticket.style.transform =
                        'scale(1)';
                }, 400);
            });
        }

        mostrarSoloPaso(
            'resultadoHtml'
        );

        irPaso(4);

        document
            .getElementById(
                'btnReservar'
            )
            ?.classList.remove(
                'd-none'
            );

        ticket?.scrollIntoView({
            behavior: 'smooth'
        });
    }
);

function abrirModalSalir() {
    const modalSalirElement =
        document.getElementById(
            'modalSalir'
        );

    if (modalSalirElement) {
        bootstrap.Modal
            .getOrCreateInstance(
                modalSalirElement
            )
            .show();
    }
}