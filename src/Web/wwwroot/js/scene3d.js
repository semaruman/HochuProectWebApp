/**
 * Hero 3D — lazy-loaded engineering object (Three.js via CDN import map).
 * Falls back to CSS gradient if WebGL unavailable or reduced motion.
 */
export function initHeroScene(container) {
  if (!container) return;

  const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const coarse = window.matchMedia("(pointer: coarse)").matches;
  const lowPower = navigator.hardwareConcurrency && navigator.hardwareConcurrency < 4;

  if (reduced || coarse || lowPower) {
    container.innerHTML = `<div class="hero__canvas-fallback" aria-hidden="true"></div>`;
    return;
  }

  import("three").then((THREE) => {
    const canvas = document.createElement("canvas");
    canvas.className = "hero__canvas";
    canvas.setAttribute("aria-hidden", "true");
    container.appendChild(canvas);

    const renderer = new THREE.WebGLRenderer({
      canvas,
      alpha: true,
      antialias: true,
      powerPreference: "high-performance"
    });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.setClearColor(0x000000, 0);
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.1;

    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(42, 1, 0.1, 100);
    camera.position.set(0, 0.2, 4.2);

    const ambient = new THREE.AmbientLight(0xffffff, 0.35);
    const key = new THREE.DirectionalLight(0x5eead4, 1.2);
    key.position.set(3, 4, 5);
    const rim = new THREE.DirectionalLight(0x7dd3fc, 0.8);
    rim.position.set(-4, -1, -2);
    scene.add(ambient, key, rim);

    const group = new THREE.Group();

    const coreGeo = new THREE.IcosahedronGeometry(1.05, 1);
    const coreMat = new THREE.MeshPhysicalMaterial({
      color: 0x0a1628,
      metalness: 0.92,
      roughness: 0.18,
      clearcoat: 1,
      clearcoatRoughness: 0.08,
      envMapIntensity: 0.6
    });
    const core = new THREE.Mesh(coreGeo, coreMat);
    group.add(core);

    const wireGeo = new THREE.IcosahedronGeometry(1.18, 2);
    const wireMat = new THREE.MeshBasicMaterial({
      color: 0x5eead4,
      wireframe: true,
      transparent: true,
      opacity: 0.22
    });
    const wire = new THREE.Mesh(wireGeo, wireMat);
    group.add(wire);

    const ringGeo = new THREE.TorusGeometry(1.55, 0.018, 8, 128);
    const ringMat = new THREE.MeshBasicMaterial({ color: 0x67e8f9, transparent: true, opacity: 0.45 });
    const ring = new THREE.Mesh(ringGeo, ringMat);
    ring.rotation.x = Math.PI / 2.4;
    group.add(ring);

    const ring2 = ring.clone();
    ring2.scale.set(0.82, 0.82, 0.82);
    ring2.rotation.x = Math.PI / 3;
    ring2.rotation.z = 0.4;
    group.add(ring2);

    scene.add(group);

    let mouseX = 0;
    let mouseY = 0;
    let targetX = 0;
    let targetY = 0;

    window.addEventListener("mousemove", (e) => {
      mouseX = (e.clientX / window.innerWidth - 0.5) * 2;
      mouseY = (e.clientY / window.innerHeight - 0.5) * 2;
    }, { passive: true });

    const resize = () => {
      const w = container.clientWidth;
      const h = container.clientHeight;
      if (!w || !h) return;
      camera.aspect = w / h;
      camera.updateProjectionMatrix();
      renderer.setSize(w, h, false);
    };

    const ro = new ResizeObserver(resize);
    ro.observe(container);
    resize();

    let t = 0;
    const animate = () => {
      requestAnimationFrame(animate);
      t += 0.004;
      targetX += (mouseX - targetX) * 0.04;
      targetY += (mouseY - targetY) * 0.04;

      group.rotation.y = t * 0.6 + targetX * 0.35;
      group.rotation.x = targetY * 0.2;
      wire.rotation.y = -t * 0.3;
      ring.rotation.z = t * 0.15;
      ring2.rotation.z = -t * 0.12;

      renderer.render(scene, camera);
    };
    animate();
  }).catch(() => {
    container.innerHTML = `<div class="hero__canvas-fallback" aria-hidden="true"></div>`;
  });
}
