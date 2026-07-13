document.addEventListener("DOMContentLoaded", function () {
    const selector = document.getElementById("filtroRapido");
    const items = document.querySelectorAll(".item-animado");

    selector.addEventListener("change", function () {
        const filtro = this.value;

        items.forEach(item => {
            const nombre = item.querySelector("h5").innerText.toUpperCase();
            let visible = (filtro === "TODOS");

            if (filtro === "CORTINAS" && nombre.includes("TUBO ALUMINIO")) visible = true;
            if (filtro === "PESADO" && nombre.includes("PESADO")) visible = true;

            // --- AGREGA ESTA LÍNEA AQUÍ ---
            if (filtro === "REGLAS" && nombre.includes("REGLA")) visible = true;
            if (filtro === "CANTONERAS" && nombre.includes("CANTONERA")) visible = true;
            if (filtro === "ACC_CORTINA" && (nombre.includes("TERMINAL") || nombre.includes("PASANTE"))) visible = true;
            if (filtro === "ACC_CORTINA" && (nombre.includes("BRIDAS") || nombre.includes("ACERADO"))) visible = true;
            if (filtro === "SEGURIDAD" && (nombre.includes("CHAPA") || nombre.includes("SEGURO"))) visible = true;
            if (filtro === "ACC_CORTINA" && (nombre.includes("AVIERTO") || nombre.includes("BRIDA"))) visible = true;
            if (filtro === "ARC_RODOPLAST" && nombre.includes("RODOPLAST")) visible = true;
            if (filtro === "ARC_TACERO" && nombre.includes("TUBO ACERADO")) visible = true;
            if (filtro === "ARC_RIELCOR" && nombre.includes("RIEL DE CORTINA")) visible = true;
            if (filtro === "ARC_CROMADO" && nombre.includes("CROMADO")) visible = true;
            if (filtro === "ARC_RIELCOR" && nombre.includes("UÑERA")) visible = true;
            if (filtro === "ARC_RIELCOR" && nombre.includes("TERMINAL")) visible = true;
            if (filtro === "ARC_RIELCOR" && nombre.includes("CRUCE")) visible = true;

            if (visible) {
                item.style.display = "block";
                setTimeout(() => {
                    item.style.opacity = "1";
                    item.style.transform = "none"; // Se asegura de no aplicar transformaciones
                }, 50);
            } else {
                item.style.opacity = "0";
                setTimeout(() => { item.style.display = "none"; }, 400);
            }
        });
    });
});