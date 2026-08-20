# ADR-006 — Cache en memoria por convención de nombre, con exclusiones

**Estado:** Aceptado (vigente) · **Tipo:** Retroactivo

## Contexto

Consultas de catálogo (productos, marcas, categorías) se repiten con alta
frecuencia y cambian con poca frecuencia relativa — son candidatas
naturales a cache. Otras consultas (carrito, dashboard, wishlist, loyalty)
son inherentemente específicas de un momento o de alta variabilidad por
usuario.

## Evidencia

**HECHO**, verificado en `CachingBehavior.cs:25-72`: el behavior actúa
únicamente sobre requests cuyo **nombre de tipo termina en `"Query"`**, con
una lista explícita de exclusión por namespace: `ShoppingCart`,
`Dashboard`, `Wishlist`, `Loyalty`. Para todo lo demás, construye una clave
de cache a partir del tipo completo + id de usuario actual + el request
serializado a JSON, consulta `ICacheService` (implementado como
`MemoryCacheService`, in-memory, no distribuido), y en caso de fallo
(*miss*) ejecuta el handler y guarda el resultado por 5 minutos.

## Decisión

Cachear automáticamente toda query (por convención de nombre) durante 5
minutos en memoria del proceso, excluyendo explícitamente los cuatro
dominios donde el dato es demasiado volátil o específico del usuario para
que valga la pena, o donde cachear introduciría el riesgo de servir datos
obsoletos en un flujo sensible (carrito, checkout-adyacente).

## Alternativas descartadas

**INFERENCIA** (reconstrucción razonada) para la estrategia general, salvo
la exclusión de dominios que sí tiene evidencia directa:

- **Cache distribuido (Redis)** en vez de `IMemoryCache`. Sería necesario
  si el backend corriera en más de una instancia — hoy el despliegue en
  Render (ver `01-Architecture/SYSTEM-ARCHITECTURE.md`) no confirma
  múltiples instancias, así que el cache en memoria de proceso es
  suficiente mientras eso no cambie. Si se escala horizontalmente, esta
  decisión debe revisarse (una instancia podría servir datos cacheados que
  otra ya invalidó).
- **Cache por entidad con invalidación explícita** (invalidar la clave de
  "producto X" cuando se actualiza el producto X) en vez de expiración por
  tiempo fijo (5 minutos). Más preciso pero requiere que cada comando de
  escritura sepa exactamente qué claves de lectura invalidar — mayor
  acoplamiento entre comandos y queries.

**HECHO, no descartada sino documentada como decisión consciente:** la
exclusión de `ShoppingCart`/`Dashboard`/`Wishlist`/`Loyalty` corresponde al
registro histórico del proyecto ("Caché ⏭️ Omitido deliberadamente; cubierto
por 7.2" en el historial de commits) — no es un olvido, es una decisión
tomada y luego reconstruida aquí con su razón más explícita.

## Consecuencia

**Se gana:** las queries de catálogo (el tráfico más frecuente de una
tienda) se sirven desde memoria la mayoría del tiempo sin tocar PostgreSQL,
sin que cada query nueva tenga que implementar su propio cache.

**Se sacrifica:** (a) mismo riesgo de convención-por-nombre que ADR-002/003
— una query mal nombrada activa o desactiva cache silenciosamente; (b) una
ventana de hasta 5 minutos donde un catálogo recién editado en el admin
puede seguir mostrando el dato viejo a algunos usuarios — aceptable para
catálogo, pero es la razón concreta por la que dashboard/carrito/wishlist/
loyalty están excluidos explícitamente, no un descuido.
