document.addEventListener("DOMContentLoaded", function () {
    const MAX_METROS = 5.95;
    let activeDraggingContainer = null;

    // Elementos Maestros de la UI
    const variantButtons = document.querySelectorAll(".js-variant-btn");
    const masterProductImg = document.getElementById("masterProductImg");
    const masterProductPlaceholder = document.getElementById("masterProductPlaceholder");
    const zoomImageAlertText = document.getElementById("zoomImageAlertText");
    const masterStockBadge = document.getElementById("masterStockBadge");
    const masterPriceInteger = document.getElementById("masterPriceInteger");
    const masterPriceDecimal = document.getElementById("masterPriceDecimal");
    const masterUnitDisplay = document.getElementById("masterUnitDisplay");
    const hiddenProductId = document.getElementById("hiddenProductId");
    const hiddenTipoVenta = document.getElementById("hiddenTipoVenta");
    const hiddenQuantityFinal = document.getElementById("hiddenQuantityFinal");

    // Elementos de Selectores y Contadores
    const unitSelectorSection = document.getElementById("unitSelectorSection");
    const wrapperVarilla = document.getElementById("wrapperVarilla");
    const wrapperMetro = document.getElementById("wrapperMetro");
    const inputVarillaQty = document.getElementById("inputVarillaQty");
    const inputM = document.getElementById("inputM");
    const inputCm = document.getElementById("inputCm");
    const masterProgressBar = document.getElementById("masterProgressBar");
    const masterProgressText = document.getElementById("masterProgressText");
    const masterSubmitCartBtn = document.getElementById("masterSubmitCartBtn");

    let selectedDataset = null;

    /* ============================================================
1. CONTROLADOR DE CAMBIO DE VARIANTE (BLINDADO CONTRA CLICS RÁPIDOS)
============================================================ */
    function switchProductVariant(button) {
        // 📱 CIERRE AUTOMÁTICO DEL BOTTOM SHEET EN MÓVIL AL CAMBIAR DIMENSIÓN
        if (typeof bootstrap !== 'undefined' && window.innerWidth < 992) {
            const modalEl = document.getElementById('bottomSheetMedidas');
            if (modalEl) {
                const modalInstance = bootstrap.Modal.getInstance(modalEl);
                if (modalInstance) modalInstance.hide();
            }
        }

        // Si la píldora ya está activa o el contenedor está cargando, bloquear re-clics
        if (button.classList.contains("active") || button.style.pointerEvents === "none") return;

        variantButtons.forEach(btn => btn.classList.remove("active"));
        button.classList.add("active");

        selectedDataset = button.dataset;
        hiddenProductId.value = selectedDataset.id;

        // --- 🛡️ INICIO DE CARGA DE INTERFAZ DE USUARIO ---
        const imageLoader = document.getElementById("productImageBoxLoader");
        const priceContainer = document.querySelector(".price-display-clean");


        // 🚫 MAXIMA SEGURIDAD: Bloquear y difuminar el acceso rápido al medidor
        const btnCustomizeMobile = document.querySelector(".btn-outline-customize-mobile");
        if (btnCustomizeMobile) {
            btnCustomizeMobile.style.pointerEvents = "none";
            btnCustomizeMobile.style.opacity = "0.6";
        }

        const mobileSummaryText = document.getElementById("mobileSummaryText");
        if (mobileSummaryText) {
            mobileSummaryText.innerHTML = `⏳ <strong>Actualizando medidor...</strong>`;
        }

        // Bloquear también el cuerpo del medidor por si está visible (PC)
        if (unitSelectorSection) {
            unitSelectorSection.style.pointerEvents = "none";
            unitSelectorSection.style.opacity = "0.5";
        }

        // 🚫 DEFENSA: Deshabilitar temporalmente TODAS las píldoras para evitar condiciones de carrera
        variantButtons.forEach(btn => {
            btn.style.pointerEvents = "none";
            btn.style.opacity = "0.6";
        });

        // Activar Loader Visual de la imagen
        if (imageLoader) imageLoader.classList.remove("d-none");

        // Difuminar y marcar precio como "Actualizando" (PC y Móvil)
        if (priceContainer) priceContainer.style.opacity = "0.3";

        const priceMobileContainer = document.querySelector(".price-badge-mobile");
        if (priceMobileContainer) priceMobileContainer.style.opacity = "0.3";

        // 🟢 Declaración correcta antes de usar su opacidad en 0.3
        const priceModalContainer = document.querySelector(".modal-price-display");
        if (priceModalContainer) priceModalContainer.style.opacity = "0.3";

        // Bloquear Botón de pedido con texto dinámico
        masterSubmitCartBtn.disabled = true;
        masterSubmitCartBtn.innerHTML = `
            <div class="spinner-border spinner-border-sm text-light" role="status"></div>
            <span>Procesando cambio...</span>
        `;

        // 1. Promesa de tiempo mínimo obligatorio de 1.5 Segundos
        const minTimePromise = new Promise(resolve => setTimeout(resolve, 1500));

        // 2. Promesa encargada de verificar si la imagen se descargó por completo del servidor
        const rutaImg = selectedDataset.imagen;
        const esImagenValida = rutaImg && rutaImg.trim() !== "" && !rutaImg.toLowerCase().includes("none.jpg");

        // Guardamos una marca de tiempo o ID único de la variante actual para este hilo de ejecución
        const currentProductContextId = selectedDataset.id;

        const imageLoadPromise = new Promise(resolve => {
            if (!esImagenValida) {
                resolve();
            } else {
                const tempImg = new Image();
                tempImg.src = rutaImg;
                if (tempImg.complete) {
                    resolve();
                } else {
                    tempImg.onload = () => resolve();
                    tempImg.onerror = () => resolve();
                }
            }
        });

        // ⏳ ESPERAR A QUE AMBAS PROMESAS SE CUMPLAN
        Promise.all([minTimePromise, imageLoadPromise]).then(() => {

            // 🚨 VERIFICACIÓN ANTICORRUPCIÓN
            if (hiddenProductId.value !== currentProductContextId) return;

            // 🔄 AJUSTE DE IMAGEN FINAL
            if (!esImagenValida) {
                masterProductImg.classList.add("d-none");
                zoomImageAlertText.classList.add("d-none");
                masterProductPlaceholder.classList.remove("d-none");
                masterProductPlaceholder.classList.add("d-flex");
            } else {
                masterProductPlaceholder.classList.add("d-none");
                masterProductPlaceholder.classList.remove("d-flex");
                masterProductImg.src = rutaImg;
                masterProductImg.classList.remove("d-none");
                zoomImageAlertText.classList.remove("d-none");
            }

            // CONTROL DE INTERFAZ: Pestañas según Tipo Unidad
            const radioVarillaLabel = document.querySelector('label[for="radioVarilla"]');
            const grupoRadios = document.querySelector('.btn-group.w-100');

            if (selectedDataset.unidad === "VARILLAS" || selectedDataset.unidad === "UNIDADES") {
                unitSelectorSection.classList.remove("d-none");
                if (selectedDataset.unidad === "UNIDADES") {
                    if (grupoRadios) grupoRadios.classList.add("d-none");
                    if (radioVarillaLabel) radioVarillaLabel.innerText = "Cantidad (Unidades)";
                } else {
                    if (grupoRadios) grupoRadios.classList.remove("d-none");
                    if (radioVarillaLabel) radioVarillaLabel.innerText = "Varilla";
                }
            } else {
                unitSelectorSection.classList.add("d-none");
            }

            // Evaluar el Stock del Elemento Seleccionado
            const stock = parseInt(selectedDataset.stock);
            if (stock <= 0) {
                masterStockBadge.className = "badge-sticker sticker-danger";
                masterStockBadge.innerText = "Agotado";
                masterSubmitCartBtn.disabled = true;
                masterSubmitCartBtn.className = "btn btn-no-stock w-100 py-3 mt-2";
                masterSubmitCartBtn.innerHTML = "No disponible";
            } else {
                masterSubmitCartBtn.disabled = false;
                masterSubmitCartBtn.className = "btn btn-add-cart w-100 py-3 mt-2";
                masterSubmitCartBtn.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" fill="currentColor" viewBox="0 0 16 16"><path d="M0 1.5A.5.5 0 0 1 .5 1H2a.5.5 0 0 1 .485.379L2.89 3H14.5a.5.5 0 0 1 .491.592l-1.5 8A.5.5 0 0 1 13 12H4a.5.5 0 0 1-.491-.408L2.01 3.607 1.61 2H.5a.5.5 0 0 1-.5-.5zM3.102 4l1.313 7h8.17l1.313-7H3.102zM5 12a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm7 0a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm-7 1a1 1 0 1 1 0 2 1 1 0 0 1 0-2zm7 0a1 1 0 1 1 0 2 1 1 0 0 1 0-2z"/></svg> <span>Agregar al pedido</span>`;

                if (stock < 5) {
                    masterStockBadge.className = "badge-sticker sticker-warning";
                    masterStockBadge.innerText = "Poco stock";
                } else {
                    masterStockBadge.className = "badge-sticker sticker-success";
                    masterStockBadge.innerText = "Disponible";
                }
            }

            // RESETEO INTELIGENTE de cantidades
            document.getElementById("radioVarilla").checked = true;
            if (selectedDataset.unidad === "VARILLAS") {
                hiddenTipoVenta.value = "VARILLA";
            } else {
                hiddenTipoVenta.value = "UNIDAD";
            }

            wrapperVarilla.classList.remove("d-none");
            wrapperMetro.classList.add("d-none");

            inputVarillaQty.value = 1;
            inputM.value = 0;
            inputCm.value = 1;

            // Renderizar precios finales
            refreshPriceDisplay();

            // --- 🏁 REMOVER ESTADOS DE CARGA Y LIBERAR PÍLDORAS ---
            if (imageLoader) imageLoader.classList.add("d-none");
            if (priceContainer) priceContainer.style.opacity = "1";
            if (priceMobileContainer) priceMobileContainer.style.opacity = "1";

            const priceModalContainerFinal = document.querySelector(".modal-price-display");
            if (priceModalContainerFinal) priceModalContainerFinal.style.opacity = "1";

            // 🔓 LIBERAR el botón de acceso rápido móvil
            if (btnCustomizeMobile) {
                btnCustomizeMobile.style.pointerEvents = "auto";
                btnCustomizeMobile.style.opacity = "1";
            }

            // Liberar el cuerpo del medidor (PC)
            if (unitSelectorSection) {
                unitSelectorSection.style.pointerEvents = "auto";
                unitSelectorSection.style.opacity = "1";
            }

            // 🔓 LIBERAR botones para que puedan volver a ser cliqueados con normalidad
            variantButtons.forEach(btn => {
                btn.style.pointerEvents = "auto";
                btn.style.opacity = "1";
            });
        });
    }

    /* ============================================================
       2. REFRESCAR EL BLOQUE DE PRECIOS SEGÚN EL TIPO DE COMPRA
       ============================================================ */
    function refreshPriceDisplay() {
        if (!selectedDataset) return;

        let precioCompleto = "0.00";
        if (hiddenTipoVenta.value === "METRO") {
            precioCompleto = selectedDataset.precioMetro;
            masterUnitDisplay.innerText = "/ METRO";
        } else {
            precioCompleto = selectedDataset.precioVarilla;
            masterUnitDisplay.innerText = selectedDataset.unidad === "VARILLAS" ? "/ VARILLA" : "/ UNIDAD";
        }

        const partes = precioCompleto.split(".");
        masterPriceInteger.innerText = partes[0];
        masterPriceDecimal.innerText = "." + partes[1];

        // 📱 MODIFICACIÓN: ACTUALIZAR EL NUEVO PRECIO MÓVIL DENTRO DE LA TARJETA EN PARALELO
        const masterPriceIntegerMobile = document.getElementById("masterPriceIntegerMobile");
        const masterPriceDecimalMobile = document.getElementById("masterPriceDecimalMobile");
        const masterUnitDisplayMobile = document.getElementById("masterUnitDisplayMobile");

        if (masterPriceIntegerMobile) masterPriceIntegerMobile.innerText = partes[0];
        if (masterPriceDecimalMobile) masterPriceDecimalMobile.innerText = "." + partes[1];
        if (masterUnitDisplayMobile) masterUnitDisplayMobile.innerText = masterUnitDisplay.innerText;

        // 📏 ACTUALIZACIÓN DINÁMICA PRECIO INTERNO DEL BOTTOM SHEET MÓVIL
        const masterPriceIntegerModal = document.getElementById("masterPriceIntegerModal");
        const masterPriceDecimalModal = document.getElementById("masterPriceDecimalModal");

        if (masterPriceIntegerModal) masterPriceIntegerModal.innerText = partes[0];
        if (masterPriceDecimalModal) masterPriceDecimalModal.innerText = "." + partes[1];
        // 🟢 La línea de masterUnitDisplayModal la borramos para que no busque un ID inexistente

        // 📏 CAMBIO DINÁMICO DE TEXTO ACLARATORIO EN MÓVIL
        const masterLabelModal = document.getElementById("masterLabelModal");
        if (masterLabelModal) {
            if (hiddenTipoVenta.value === "METRO") {
                masterLabelModal.innerText = "Precio por Metro:";
            } else {
                masterLabelModal.innerText = selectedDataset.unidad === "VARILLAS" ? "Precio por Varilla:" : "Precio por Unidad:";
            }
        }
        // Sincronizar cantidad final
        if (hiddenTipoVenta.value === "METRO") {
            let m = parseInt(inputM.value) || 0;
            let cm = parseInt(inputCm.value) || 0;
            let total = m + (cm / 100);
            if (total > MAX_METROS) { total = MAX_METROS; inputM.value = 5; inputCm.value = 95; }
            if (total < 0.01) total = 0.01;

            hiddenQuantityFinal.value = total.toFixed(2);
            masterProgressBar.style.width = (total / MAX_METROS * 100) + "%";
            masterProgressText.innerText = total.toFixed(2) + "m";
        } else {
            hiddenQuantityFinal.value = parseInt(inputVarillaQty.value) || 1;
        }

        // Sincronizar el texto del botón exterior de acceso rápido en móvil
        const mobileSummaryText = document.getElementById("mobileSummaryText");
        if (mobileSummaryText) {
            if (hiddenTipoVenta.value === "METRO") {
                mobileSummaryText.innerHTML = `Corte personalizado a: <strong>${hiddenQuantityFinal.value} metros</strong>`;
                mobileSummaryText.className = "d-block text-success small";
            } else {
                const unidadTexto = selectedDataset.unidad === "VARILLAS" ? "varilla(s)" : "unidad(es)";
                mobileSummaryText.innerHTML = `Cantidad seleccionada: <strong>${hiddenQuantityFinal.value} ${unidadTexto}</strong>`;
                mobileSummaryText.className = "d-block text-dark small";
            }
        }
    }

    variantButtons.forEach(btn => {
        btn.addEventListener("click", function () { switchProductVariant(this); });
    });

    if (variantButtons.length > 0) switchProductVariant(variantButtons[0]);

    /* ============================================================
       3. INTERCAMBIO DINÁMICO DE RADIOS (VARILLA / METROS)
       ============================================================ */
    document.querySelectorAll(".js-radio-tipo").forEach(radio => {
        radio.addEventListener("change", function () {
            if (this.value === "METRO") {
                hiddenTipoVenta.value = "METRO";
                wrapperVarilla.classList.add("d-none");
                wrapperMetro.classList.remove("d-none");
            } else {
                hiddenTipoVenta.value = "VARILLA";
                wrapperVarilla.classList.remove("d-none");
                wrapperMetro.classList.add("d-none");
            }
            refreshPriceDisplay();
        });
    });

    /* ============================================================
       4. LÓGICA DE CONTADORES STEPPERS
       ============================================================ */
    document.getElementById("btnPlusVarilla").addEventListener("click", () => { inputVarillaQty.value = parseInt(inputVarillaQty.value) + 1; refreshPriceDisplay(); });
    document.getElementById("btnMinusVarilla").addEventListener("click", () => { if (parseInt(inputVarillaQty.value) > 1) { inputVarillaQty.value = parseInt(inputVarillaQty.value) - 1; refreshPriceDisplay(); } });

    document.getElementById("btnPlusM").addEventListener("click", () => { inputM.value = parseInt(inputM.value) + 1; refreshPriceDisplay(); });
    document.getElementById("btnMinusM").addEventListener("click", () => { if (parseInt(inputM.value) > 0) { inputM.value = parseInt(inputM.value) - 1; refreshPriceDisplay(); } });

    document.getElementById("btnPlusCm").addEventListener("click", () => { let val = parseInt(inputCm.value); inputCm.value = val >= 99 ? 0 : val + 1; refreshPriceDisplay(); });
    document.getElementById("btnMinusCm").addEventListener("click", () => { let val = parseInt(inputCm.value); inputCm.value = val <= 0 ? 99 : val - 1; refreshPriceDisplay(); });

    /* ============================================================
       5. LÓGICA COMPLETA DEL DESPLAZAMIENTO DEL RIEL (MOUSE/TOUCH)
       ============================================================ */
    function handleSlider(e, container) {
        const rect = container.getBoundingClientRect();
        let clientX = e.clientX || (e.touches ? e.touches[0].clientX : 0);
        let widthPercent = Math.max(0, Math.min(100, ((clientX - rect.left) / rect.width) * 100));

        let totalMedida = (widthPercent / 100) * MAX_METROS;
        inputM.value = Math.floor(totalMedida);
        inputCm.value = Math.min(99, Math.round((totalMedida % 1) * 100));

        refreshPriceDisplay();
    }

    const sliderContainer = document.getElementById("progressBarContainer");
    sliderContainer.addEventListener("mousedown", function (e) { activeDraggingContainer = sliderContainer; handleSlider(e, sliderContainer); });
    document.addEventListener("mousemove", (e) => { if (activeDraggingContainer) handleSlider(e, activeDraggingContainer); });
    document.addEventListener("mouseup", () => { activeDraggingContainer = null; });

    sliderContainer.addEventListener("touchstart", function (e) { e.preventDefault(); activeDraggingContainer = sliderContainer; handleSlider(e, sliderContainer); }, { passive: false });
    document.addEventListener("touchmove", function (e) { if (activeDraggingContainer) handleSlider(e, activeDraggingContainer); }, { passive: false });
    document.addEventListener("touchend", () => { activeDraggingContainer = null; });

    // Entrada de escritura directa desde teclado
    const inputSincronizados = [inputM, inputCm];
    inputSincronizados.forEach(inp => {
        inp.addEventListener("input", refreshPriceDisplay);
    });

    /* ============================================================
       6. ENVÍO AJAX DEL CARRITO + ANIMACIÓN DE VUELO CON FILTRO
       ============================================================ */
    document.getElementById("masterAddCartForm").addEventListener("submit", async function (e) {
        e.preventDefault();
        const form = this;
        const button = masterSubmitCartBtn;
        let cartTarget = [document.getElementById("toggleMiniCart"), document.getElementById("mobileCartTarget")].find(el => el && el.offsetWidth > 0);

        const formData = new FormData(form);

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const result = await response.json();
            if (result.success) {
                if (typeof CarritoUI !== 'undefined') { CarritoUI.actualizarContadores(result.totalItems); }
                const originalContent = button.innerHTML;
                button.innerHTML = "<span>✔️ ¡Agregado con éxito!</span>";
                button.disabled = true;

                const cardContainer = document.querySelector('.main-product-image-box');
                if (cardContainer && cartTarget) {
                    ejecutarAnimacionVuelo(cardContainer, cartTarget);
                }

                setTimeout(() => {
                    button.innerHTML = originalContent;
                    button.disabled = false;
                }, 2000);
            }
        } catch (error) { console.error("Error:", error); }
    });

    /* 🚀 FUNCIÓN DE VUELO PREMIUM OPTIMIZADA PARA MÓVIL */
    function ejecutarAnimacionVuelo(image, target) {
        const card = document.querySelector('.main-product-image-box');
        if (!card) return;

        const rectCard = card.getBoundingClientRect();
        const rectCar = target.getBoundingClientRect();

        const scrollTop = window.scrollY || document.documentElement.scrollTop;
        const scrollLeft = window.scrollX || document.documentElement.scrollLeft;

        const inicioTop = rectCard.top + scrollTop;
        const inicioLeft = rectCard.left + scrollLeft;

        const finTop = (rectCar.top + (rectCar.height / 2)) + scrollTop;
        const finLeft = (rectCar.left + (rectCar.width / 2)) + scrollLeft;

        const bolitaVoladora = document.createElement('div');
        bolitaVoladora.className = 'tarjeta-convertida-bolita';

        bolitaVoladora.style.background = 'linear-gradient(135deg, #ff6f00 0%, #e66400 100%)';
        bolitaVoladora.style.boxShadow = '0 10px 30px rgba(255, 111, 0, 0.6)';

        bolitaVoladora.style.position = 'absolute';
        bolitaVoladora.style.top = inicioTop + 'px';
        bolitaVoladora.style.left = inicioLeft + 'px';
        bolitaVoladora.style.width = rectCard.width + 'px';
        bolitaVoladora.style.height = rectCard.height + 'px';
        bolitaVoladora.style.transform = 'scale(1) rotate(0deg)';

        document.body.appendChild(bolitaVoladora);

        requestAnimationFrame(() => {
            setTimeout(() => {
                bolitaVoladora.style.top = finTop + 'px';
                bolitaVoladora.style.left = finLeft + 'px';
                bolitaVoladora.style.width = '24px';
                bolitaVoladora.style.height = '24px';
                bolitaVoladora.style.borderRadius = '50%';
                bolitaVoladora.style.transform = 'scale(0.4) rotate(180deg)';
            }, 50);
        });

        setTimeout(() => {
            bolitaVoladora.remove();
            target.classList.add("cart-animate");
            setTimeout(() => target.classList.remove("cart-animate"), 500);
        }, 700);
    }

    /* ============================================================
       7. LIGHTBOX CONTROLES DE SEGURIDAD
       ============================================================ */
    const modalZoomEl = document.getElementById('modalZoomImagen');
    const imagenDestino = document.getElementById('imagenZoomada');
    if (modalZoomEl) {
        const modalZoom = new bootstrap.Modal(modalZoomEl);
        masterProductImg.addEventListener('click', function () {
            if (!this.classList.contains("d-none")) {
                imagenDestino.src = this.src;
                imagenDestino.alt = this.alt;
                modalZoom.show();
            }
        });
    }
});