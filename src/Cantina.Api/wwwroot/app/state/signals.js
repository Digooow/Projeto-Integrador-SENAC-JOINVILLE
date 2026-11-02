/**
 * CAMADA 2 (STATE) — Motor Reativo de Signals (Padrão Angular Signals / TC39 Signals)
 * Permite reatividade declarativa fina (signal, computed, effect) sem re-renderizar o DOM inteiro.
 */

let activeListener = null;

export function signal(initialValue) {
  let value = initialValue;
  const subscribers = new Set();

  const read = () => {
    if (activeListener) {
      subscribers.add(activeListener);
    }
    return value;
  };

  read.set = (nextValue) => {
    if (Object.is(value, nextValue)) return;
    value = nextValue;
    for (const sub of [...subscribers]) {
      sub();
    }
  };

  read.update = (updater) => {
    read.set(updater(value));
  };

  return read;
}

export function computed(derivationFn) {
  const derived = signal(undefined);
  effect(() => {
    derived.set(derivationFn());
  });
  return derived;
}

export function effect(effectFn) {
  const execute = () => {
    const prev = activeListener;
    activeListener = execute;
    try {
      effectFn();
    } finally {
      activeListener = prev;
    }
  };
  execute();
}

