void (async () => {
const THREE = await import("/lib/three/three.module.min.js");

const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
const state = window.__barberThreeScene ??= { active: null, bound: false, observer: null };

function clamp(value, min, max) {
    return Math.min(Math.max(value, min), max);
}

function disposeScene() {
    const active = state.active;
    if (!active) {
        return;
    }

    active.dispose();
    state.active = null;
}

function createScissors() {
    const scissors = new THREE.Group();
    const metal = new THREE.MeshStandardMaterial({ color: 0xe2eeeb, metalness: 0.9, roughness: 0.18 });
    const grip = new THREE.MeshStandardMaterial({ color: 0x17695e, metalness: 0.35, roughness: 0.35 });
    const bladeGeometry = new THREE.BoxGeometry(0.16, 2.36, 0.11);
    const stemGeometry = new THREE.BoxGeometry(0.11, 0.72, 0.13);
    const ringGeometry = new THREE.TorusGeometry(0.31, 0.075, 10, 28);

    bladeGeometry.translate(0, 1.06, 0);

    function createArm() {
        const arm = new THREE.Group();
        const blade = new THREE.Mesh(bladeGeometry, metal);
        const stem = new THREE.Mesh(stemGeometry, metal);
        const ring = new THREE.Mesh(ringGeometry, grip);

        blade.castShadow = true;
        stem.position.y = -0.35;
        ring.position.y = -0.97;
        arm.add(blade, stem, ring);
        return arm;
    }

    const leftArm = createArm();
    const rightArm = createArm();
    const screw = new THREE.Mesh(
        new THREE.SphereGeometry(0.13, 18, 18),
        new THREE.MeshStandardMaterial({ color: 0xc58c3d, metalness: 0.8, roughness: 0.25 })
    );

    scissors.add(leftArm, rightArm, screw);
    scissors.userData = { leftArm, rightArm };
    return scissors;
}

function createComb() {
    const comb = new THREE.Group();
    const material = new THREE.MeshStandardMaterial({ color: 0x17342f, metalness: 0.2, roughness: 0.42 });
    const spine = new THREE.Mesh(new THREE.BoxGeometry(2.8, 0.14, 0.13), material);

    comb.add(spine);

    for (let index = 0; index < 16; index += 1) {
        const tooth = new THREE.Mesh(new THREE.BoxGeometry(0.045, 0.48, 0.08), material);
        tooth.position.set(-1.23 + index * 0.164, -0.29, 0);
        comb.add(tooth);
    }

    comb.position.set(-1.85, -1.55, -0.55);
    comb.rotation.set(-0.22, 0.18, 0.3);
    return comb;
}

function createHairField() {
    const field = new THREE.Group();
    const colors = [0x24180f, 0x5a321d, 0x9b6336, 0xd0a46f];

    for (let index = 0; index < 18; index += 1) {
        const x = -2.85 + index * 0.33;
        const y = 1.95 + (index % 4) * 0.12;
        const curve = new THREE.CatmullRomCurve3([
            new THREE.Vector3(x, y, -0.75),
            new THREE.Vector3(x + 0.18, y - 0.75, -0.25),
            new THREE.Vector3(x - 0.16, y - 1.4, 0.18),
            new THREE.Vector3(x + 0.11, y - 2.12, 0.4)
        ]);
        const geometry = new THREE.BufferGeometry().setFromPoints(curve.getPoints(13));
        const material = new THREE.LineBasicMaterial({
            color: colors[index % colors.length],
            transparent: true,
            opacity: 0.84,
            depthWrite: false
        });

        field.add(new THREE.Line(geometry, material));
    }

    field.position.set(0.82, 0.25, -0.7);
    field.rotation.z = -0.12;
    return field;
}

function createHairClippings() {
    const group = new THREE.Group();
    const clippings = [];

    for (let index = 0; index < 18; index += 1) {
        const geometry = new THREE.BufferGeometry().setFromPoints([
            new THREE.Vector3(-0.1, -0.16, 0),
            new THREE.Vector3(0.11, 0.19, 0)
        ]);
        const material = new THREE.LineBasicMaterial({
            color: index % 2 === 0 ? 0x301c0f : 0x7e4828,
            transparent: true,
            opacity: 0,
            depthWrite: false
        });
        const clipping = new THREE.Line(geometry, material);

        clipping.visible = false;
        group.add(clipping);
        clippings.push({ clipping, material, life: 0, velocity: new THREE.Vector3() });
    }

    return { group, clippings };
}

function createScene(host) {
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(37, 1, 0.1, 100);
    const renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true, powerPreference: "high-performance" });
    const rig = new THREE.Group();
    const scissors = createScissors();
    const comb = createComb();
    const hairField = createHairField();
    const { group: clippingsGroup, clippings } = createHairClippings();
    const clock = new THREE.Clock();
    const interactionTarget = host.closest(".hero") ?? host;
    let frameId = 0;
    let lastRenderAt = 0;
    let isVisible = true;
    let lastSnip = 0;
    const pointer = new THREE.Vector2();
    const targetPointer = new THREE.Vector2();

    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.65));
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    renderer.domElement.setAttribute("aria-hidden", "true");
    host.append(renderer.domElement);

    camera.position.set(0, 0, 8.6);
    scene.add(new THREE.AmbientLight(0xfff1d7, 1.8));

    const keyLight = new THREE.DirectionalLight(0xf2c083, 3.2);
    keyLight.position.set(3.8, 4.6, 5.2);
    scene.add(keyLight);

    const accentLight = new THREE.PointLight(0x49a89a, 18, 12);
    accentLight.position.set(-3.4, -1.8, 3.2);
    scene.add(accentLight);

    scissors.position.set(1.34, 0.1, 0.3);
    scissors.rotation.set(-0.18, 0.24, -0.35);
    scissors.scale.setScalar(0.94);
    rig.add(hairField, comb, scissors, clippingsGroup);
    scene.add(rig);

    function resize() {
        const width = Math.max(host.clientWidth, 1);
        const height = Math.max(host.clientHeight, 1);

        camera.aspect = width / height;
        camera.updateProjectionMatrix();
        renderer.setSize(width, height, false);
    }

    function emitClippings() {
        clippings.forEach((entry, index) => {
            const spread = (index / Math.max(clippings.length - 1, 1) - 0.5) * 0.98;

            entry.life = 0.7 + (index % 4) * 0.08;
            entry.clipping.visible = true;
            entry.clipping.position.set(1.17 + spread, 0.82 + (index % 3) * 0.08, 0.5 + (index % 4) * 0.08);
            entry.clipping.rotation.set(0, 0, spread * 4.8);
            entry.velocity.set(spread * 0.68, 0.8 + (index % 5) * 0.08, 0.16 - (index % 3) * 0.08);
            entry.material.opacity = 0.86;
        });
    }

    function updateClippings(delta) {
        clippings.forEach((entry) => {
            if (entry.life <= 0) {
                return;
            }

            entry.life -= delta;
            entry.velocity.y -= 1.45 * delta;
            entry.clipping.position.addScaledVector(entry.velocity, delta);
            entry.clipping.rotation.z += 1.8 * delta;
            entry.material.opacity = Math.max(entry.life, 0) * 1.25;
            entry.clipping.visible = entry.life > 0;
        });
    }

    function onPointerMove(event) {
        const rect = interactionTarget.getBoundingClientRect();
        targetPointer.x = clamp((event.clientX - rect.left) / Math.max(rect.width, 1) - 0.5, -0.5, 0.5);
        targetPointer.y = clamp((event.clientY - rect.top) / Math.max(rect.height, 1) - 0.5, -0.5, 0.5);
    }

    function onPointerLeave() {
        targetPointer.set(0, 0);
    }

    const resizeObserver = new ResizeObserver(resize);
    const visibilityObserver = new IntersectionObserver(([entry]) => {
        isVisible = entry.isIntersecting;
    }, { threshold: 0.06 });

    resizeObserver.observe(host);
    visibilityObserver.observe(host);
    interactionTarget.addEventListener("pointermove", onPointerMove);
    interactionTarget.addEventListener("pointerleave", onPointerLeave);
    resize();

    function render(timestamp) {
        frameId = window.requestAnimationFrame(render);

        if (!isVisible || document.hidden) {
            return;
        }

        if (timestamp - lastRenderAt < 33) {
            return;
        }

        lastRenderAt = timestamp;

        const delta = Math.min(clock.getDelta(), 0.05);
        const elapsed = clock.elapsedTime;
        const snip = (Math.sin(elapsed * 1.55) + 1) / 2;
        const bladeAngle = 0.07 + (1 - snip) * 0.43;
        const heroRect = interactionTarget.getBoundingClientRect();
        const scrollShift = clamp((heroRect.top + heroRect.height * 0.5 - window.innerHeight * 0.5) / Math.max(window.innerHeight, 1), -1, 1);

        pointer.lerp(targetPointer, 0.055);
        scissors.userData.leftArm.rotation.z = bladeAngle;
        scissors.userData.rightArm.rotation.z = -bladeAngle;
        scissors.rotation.y = 0.24 + pointer.x * 0.78;
        scissors.rotation.x = -0.18 - pointer.y * 0.46;
        scissors.position.y = 0.1 + Math.sin(elapsed * 0.72) * 0.12;
        comb.rotation.z = 0.3 + Math.sin(elapsed * 0.45) * 0.045;
        hairField.rotation.y = Math.sin(elapsed * 0.58) * 0.08;
        rig.rotation.z = -0.025 + pointer.x * 0.08;
        rig.position.y = scrollShift * -0.26;

        if (snip > 0.985 && lastSnip <= 0.985) {
            emitClippings();
        }

        lastSnip = snip;
        updateClippings(delta);
        renderer.render(scene, camera);
    }

    host.classList.add("is-ready");
    frameId = window.requestAnimationFrame(render);

    return {
        host,
        dispose() {
            window.cancelAnimationFrame(frameId);
            resizeObserver.disconnect();
            visibilityObserver.disconnect();
            interactionTarget.removeEventListener("pointermove", onPointerMove);
            interactionTarget.removeEventListener("pointerleave", onPointerLeave);
            scene.traverse((object) => {
                object.geometry?.dispose();
                const materials = Array.isArray(object.material) ? object.material : [object.material];
                materials.forEach((material) => material?.dispose?.());
            });
            renderer.dispose();
            renderer.forceContextLoss();
            renderer.domElement.remove();
        }
    };
}

function initBarberScene() {
    const host = document.querySelector("[data-barber-scene]");

    if (state.active?.host === host) {
        return;
    }

    disposeScene();

    if (!host || reducedMotion.matches) {
        return;
    }

    try {
        state.active = createScene(host);
    } catch {
        host.remove();
    }
}

if (!state.bound) {
    state.bound = true;
    document.addEventListener("DOMContentLoaded", initBarberScene);
    document.addEventListener("enhancedload", initBarberScene);
    window.addEventListener("pagehide", disposeScene);
    reducedMotion.addEventListener("change", initBarberScene);
    state.observer = new MutationObserver(() => {
        const currentHost = document.querySelector("[data-barber-scene]");
        const activeHost = state.active?.host ?? null;
        if (currentHost !== activeHost) {
            window.requestAnimationFrame(initBarberScene);
        }
    });
    state.observer.observe(document.body, { childList: true, subtree: true });
}

if (document.readyState !== "loading") {
    initBarberScene();
}
})().catch(() => {
    document.querySelector("[data-barber-scene]")?.remove();
});
