# Propuesta de Investigación: Spec-Driven Agentic Programming (SDAP)

## 1. Título Tentativo
**Desarrollo de un marco metodológico, epistemología de co-evolución de contexto y estándares de gobernanza para la optimización del desarrollo de software asistido por Modelos de Lenguaje (LLMs) y Agentes Autónomos.**

---

## 2. Planteamiento del Problema
En la actualidad, la adopción de Inteligencia Artificial en el ciclo de vida del desarrollo de software (SDLC) se ha centrado en un uso intuitivo y transaccional mediante chats conversacionales o agentes autónomos que operan sin restricciones. Sin embargo, este enfoque ad-hoc e impulsado únicamente por "respuestas rápidas" padece de deficiencias estructurales en la ingeniería de software moderna:

* **Tratamiento Utilitario y Transaccional de la IA:** La mayoría de los desarrolladores usan los LLMs bajo un paradigma simplista de *Pregunta → Respuesta → Fin*. Esto ignora la capacidad de la IA como compañero de razonamiento y priva al proyecto de un proceso de exploración donde el problema y la solución puedan evolucionar conjuntamente.
* **Degradación y Deriva de Contexto (*Context Drift*):** Al saturar la ventana de atención inyectando repositorios completos o código irrelevante, se generan alucinaciones semánticas, respuestas truncadas y un gasto ineficiente de tokens debido a las limitaciones físicas del mecanismo de auto-atención (*Lost in the Middle*).
* **Falta de Gobierno Arquitectónico y Ausencia de Contexto Vivo:** Delegar objetivos de alto nivel a agentes autónomos sin límites de diseño rígidos introduce deuda técnica, código duplicado, violaciones a las reglas de negocio y destrucción de patrones del proyecto.
* **Vacío de Especificación en Casos de Uso e Interfaz (UI/UX):** La omisión de contratos formales para los actores de usuario (`USE_CASES.md`) y de la arquitectura de pantallas y navegación (`SCREEN_FLOW.md`) provoca que los agentes alucinen flujos de cliente, rutas huérfanas, modales inconsistentes y estados de vista no sincronizados.

### Definición Central de SDAP
> *"SDAP no busca obtener respuestas rápidas; busca aumentar progresivamente la calidad del contexto compartido para que el problema, el objetivo y la solución evolucionen conjuntamente hasta alcanzar una comprensión más profunda."*

**Pregunta de investigación:** ¿Cómo puede un marco metodológico basado en la co-evolución contextual del problema (Objetivo Emergente), combinado con una especificación hexagonal en Markdown (Capa 0: Genoma Raíz de 6 dimensiones), una estructura de contexto vivo (`docs/ai/`) y un protocolo de ejecución atómica multidimensional, reducir el tiempo de desarrollo, mitigar las alucinaciones de la IA y preservar la integridad arquitectónica y visual del software?

---

## 3. Índice General del Proyecto

### Capítulo I: Introducción, Fundamentos y Epistemología SDAP
* **1.1.** Introducción al Desarrollo Asistido por IA (El espectro Chat vs. Agente vs. Co-evolución).
* **1.2.** El Problema del Contexto en los Transformadores (Ventanas de contexto, costo de tokens y el fenómeno *Lost in the Middle*).
* **1.3.** Epistemología de SDAP: La Co-Evolución del Contexto Compartido y el *Objetivo Emergente*.
* **1.4.** Justificación del Estándar SDAP: Arquitectura de Doble Capa y Separación Ontológica (Motor Metodológico vs. Producto).

### Capítulo II: El Marco Metodológico SDAP (La Teoría)
* **2.1.** Fase de Co-Diseño Dialéctico (*Human-AI Inception* y el Bucle de Cuestionamiento Crítico).
* **2.2.** La Arquitectura Hexagonal de Documentación Raíz (Genoma `.sdap/` y Contexto Vivo `docs/ai/`).
* **2.3.** Modelado Visual para IAs: Mermaid como Lenguaje de Abstracción Lógica (C4, State Machine, ERD, Casos de Uso, Wireflows y Secuencia).
* **2.4.** Principio de Ejecución Atómica Multidimensional (Intersección de Flujo, Caso de Uso, Pantalla y Guardrails).

### Capítulo III: Estructuración del Contexto Vivo y Gobernanza del Repositorio (La Práctica)
* **3.1.** Visión General de la Arquitectura de Información del Repositorio.
* **3.2.** Capa 0: Gobernanza Raíz y Genoma del Proyecto (`.sdap/` - Las 6 dimensiones inmutables).
* **3.3.** Capa 1: Contexto Vivo de Código (`docs/ai/` - Índice estandarizado de 15 archivos).
* **3.4.** Documentación Local de Módulo (`INTERFACE_FLOW.md`).
* **3.5.** Ciclo de Vida del Contexto y Sincronización Inversa (*Reverse Sync*).
* **3.6.** Protocolo de Inyección Multidimensional de Contexto para Agentes.

### Capítulo IV: Protocolos de Inyección, Guía Operativa y Evaluación
* **4.1.** El Protocolo de Inyección de Contexto (*Context Injection Protocol* y Payload XML Multidimensional).
* **4.2.** Plantilla Estandarizada del Prompt de Ejecución (*The SDAP Prompt*).
* **4.3.** Casos de Uso Prácticos y Flujos de Trabajo (*Workflows* de Features, Bugs y Refactor).
* **4.4.** Sistema de Barreras (*Guardrails*) y Red-Teaming (Agente Auditor de Gobernanza).
* **4.5.** Métricas de Evaluación Cuantitativa (CRR, TEI, ACS, IRS) y Conclusiones.
