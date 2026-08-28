window.HochuEffects = (() => {
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const coarsePointer = window.matchMedia("(pointer: coarse)").matches;
  const lowPower = navigator.hardwareConcurrency && navigator.hardwareConcurrency < 4;

  function mountAmbient() {
    if (document.querySelector(".ambient")) return;

    const ambient = document.createElement("div");
    ambient.className = "ambient";
    ambient.setAttribute("aria-hidden", "true");
    ambient.innerHTML = `
      <div class="ambient__mesh"></div>
      <div class="ambient__grid"></div>
      <div class="ambient__noise"></div>`;
    document.body.prepend(ambient);

    if (!reducedMotion && !coarsePointer && !lowPower) {
      const glow = document.createElement("div");
      glow.className = "cursor-glow";
      glow.setAttribute("aria-hidden", "true");
      document.body.appendChild(glow);
      document.body.classList.add("has-cursor-glow");

      let raf = 0;
      let tx = 0;
      let ty = 0;
      document.addEventListener("mousemove", (e) => {
        tx = e.clientX;
        ty = e.clientY;
        if (raf) return;
        raf = requestAnimationFrame(() => {
          glow.style.transform = `translate(${tx}px, ${ty}px)`;
          raf = 0;
        });
      }, { passive: true });
    }
  }

  function mountScrollNav() {
    const header = document.getElementById("site-header");
    if (!header) return;

    const onScroll = () => {
      header.classList.toggle("is-scrolled", window.scrollY > 24);
    };
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
  }

  function mountReveal() {
    if (reducedMotion) return;

    const els = document.querySelectorAll(".reveal, .section, .item, .step-card, .dash-tile");
    if (!els.length) return;

    const io = new IntersectionObserver(
      (entries) => {
        entries.forEach((e) => {
          if (e.isIntersecting) {
            e.target.classList.add("is-visible");
            io.unobserve(e.target);
          }
        });
      },
      { threshold: 0.12, rootMargin: "0px 0px -40px 0px" }
    );

    els.forEach((el, i) => {
      if (!el.classList.contains("reveal")) el.classList.add("reveal");
      el.style.transitionDelay = `${Math.min(i * 40, 200)}ms`;
      io.observe(el);
    });
  }

  function mountCardTilt() {
    if (reducedMotion || coarsePointer || lowPower) return;

    document.querySelectorAll(".item.glass-card, .glass-card--tilt").forEach((card) => {
      if (card.dataset.tiltBound) return;
      card.dataset.tiltBound = "1";
      card.classList.add("is-tiltable");

      card.addEventListener("mousemove", (e) => {
        const rect = card.getBoundingClientRect();
        const x = (e.clientX - rect.left) / rect.width - 0.5;
        const y = (e.clientY - rect.top) / rect.height - 0.5;
        card.classList.add("is-tilted");
        card.style.setProperty("--tilt-x", `${x * 4}deg`);
        card.style.setProperty("--tilt-y", `${-y * 4}deg`);
        card.style.setProperty("--lift", "-4px");
      });
      card.addEventListener("mouseleave", () => {
        card.classList.remove("is-tilted");
        card.style.removeProperty("--tilt-x");
        card.style.removeProperty("--tilt-y");
        card.style.removeProperty("--lift");
      });
    });
  }

  function mountSearchShortcut() {
    document.addEventListener("keydown", (e) => {
      if (e.key !== "/" || e.ctrlKey || e.metaKey || e.altKey) return;
      const tag = e.target.tagName;
      if (tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT" || e.target.isContentEditable) return;
      const search = document.getElementById("q");
      if (!search) return;
      e.preventDefault();
      search.focus();
      search.select?.();
    });
  }

  function init() {
    if (!init._core) {
      mountAmbient();
      mountScrollNav();
      mountSearchShortcut();
      init._core = true;
    }
    mountReveal();
    mountCardTilt();
  }

  return { init, mountReveal, mountCardTilt, reducedMotion, coarsePointer, lowPower };
})();
