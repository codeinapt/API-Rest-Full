# Limitaciones conocidas y mejoras futuras

Puntos identificados durante la Inception que **no se implementarán en esta versión**, de forma consciente y justificada.

| # | Limitación / mejora | Origen | Motivo de no implementarla ahora |
|---|---|---|---|
| I-01 | **Historial de cambios de estado de producto.** Una nueva desactivación sobrescribe los datos de la anterior. Mejora: tabla `ProductStatusChanges` de solo inserción. | ADR-0006 | El libro de movimientos ya audita el stock, que es lo crítico. Se evita ampliar el alcance. |
| I-02 | **Multi-sede.** Supervisor por área *y* sede. | ADR-0008 | La versión modela un único almacén. |
| I-03 | **Ajuste por conteo físico** con control optimista (`ExpectedStock`). | ADR-0007 | El ajuste por delta es seguro ante concurrencia y suficiente para el alcance. |
| I-04 | **Paginación keyset (cursor)** para grandes volúmenes. | ADR-0011 | La paginación offset es suficiente y más simple de consumir desde Swagger. |
| I-05 | **Reactivación de áreas y categorías.** | DOMAIN_LOGIC §5.2 | Operación poco frecuente; mientras tanto se puede crear una nueva con el mismo nombre o código gracias a los índices únicos filtrados. |
| I-06 | **Concurrencia optimista en la edición de catálogo** (`rowversion`). Hoy, en ediciones simultáneas de nombre o descripción, gana la última escritura. | ADR-0014 | El stock, que es lo crítico, ya está protegido con sentencias atómicas. |
