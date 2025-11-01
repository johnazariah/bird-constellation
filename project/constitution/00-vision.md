# 🦉 The Owlet Constellation
### *An Ecosystem of Cognitive Applications for Thought, Memory, Vision, and Creation*

---

## 🌌 Vision

The **Owlet Constellation** is a family of intelligent, local-first applications designed to extend the human mind — memory, perception, reason, and craft — each embodied as a distinct yet interoperable being.

Each member is an **Aspire/.NET application**, sharing a common protocol for discovery, event exchange, and search.  
Together, they form a complete personal knowledge and creative environment:  
local, privacy-preserving, modular, and agent-ready.

---

## 🪶 The Five Core Applications

| Symbol | Name | Archetype | Tagline | Core Faculty |
|---------|------|------------|----------|----------------|
| 🦉 | **Owlet** | The Librarian | “Knows everything you keep.” | **Knowledge** — watches, extracts, and indexes. |
| 💡 | **Lumen** | The Scholar | “Thinks with your library.” | **Understanding** — analyses, summarises, writes, reasons. |
| 🦢 | **Cygnet** | The Rememberer | “Graceful recall.” | **Memory** — timelines, journaling, semantic recall. |
| 🦅 | **Eaglet** | The Visionary | “Sees everything.” | **Perception** — image indexing, tagging, visual search. |
| 🖤 | **Raven** | The Architect | “Builds from thought.” | **Creation** — automation, code, orchestration, and making. |

---

## 🧭 Guiding Principles

1. **Local First**  
   Your data never has to leave your machine. Cloud sync is optional, not default.

2. **Composable by Design**  
   Each app stands alone but speaks a shared protocol (`/search`, `/events`, `/actions`, `/tags`).

3. **Private, Persistent Intelligence**  
   Agents are local processes, not remote services. Your models learn *you*, not the internet.

4. **Human-Centered Simplicity**  
   Non-nerds can install and use Owlet, Cygnet, or Eaglet without setup; Lumen and Raven unfold naturally for advanced users.

5. **Extensible by Intention**  
   Every application is an Aspire project exposing a service manifest; adding one extends the constellation automatically.

---

## 🧩 Shared Architecture

### 1. The **Knowledge Layer** — Owlet
- Indexes all local files: docs, images, code, notes.
- Exposes:
  - `GET /search?q=...&kind=...`
  - `GET /files/{id}`
  - `GET /events?since=...`
  - `POST /tags`
- Acts as the common substrate.

### 2. The **Intelligence Layer** — Lumen, Cygnet, Eaglet
- Consume Owlet’s APIs.
- Provide domain reasoning and summarisation:
  - **Lumen**: academic and theological reasoning.
  - **Cygnet**: temporal recall and cross-context search.
  - **Eaglet**: vision embeddings and tag inference.
- Communicate via an event bus (Dapr/Aspire channels).

### 3. The **Creation Layer** — Raven
- Listens to events from Owlet and the others.
- Converts insights into *actions* and *artifacts*:
  - Generates code, documents, presentations, or tests.
  - Coordinates sub-agents (`Implement`, `Test`, `Document`).
- Integrates with GitHub, VS Code, or CI/CD pipelines.

---

## 🔗 Shared Protocols

| Function | Route | Description |
|-----------|--------|-------------|
| Search | `GET /search` | Common query interface for all content types. |
| Events | `GET /events` | Subscription feed for “new file,” “updated,” “annotated,” etc. |
| Tags | `POST /tags` | Shared tagging API across applications. |
| Actions | `POST /actions` | Task creation and coordination between assistants. |

Each assistant can discover others through Aspire’s service registry; they automatically form a mesh of cooperation.

---

## 🧠 Example Flow

1. **Owlet** indexes a new PDF “Emergence_in_Quantum_Systems.pdf.”  
2. **Lumen** detects it via `/events`, summarises the paper, and writes “summary.md.”  
3. **Cygnet** adds it to the timeline: “Read on Nov 1, 2025.”  
4. **Eaglet** tags related diagrams extracted from the PDF.  
5. **Raven** notices the keywords *simulation* and *pipeline*, generates scaffolding code, and opens a PR.  
6. The constellation glows a little brighter.

---

## 🛠️ Technical Stack

- **.NET 9 Aspire** — distributed app orchestration  
- **Postgres + pgvector / SQLite** — embeddings & indexes  
- **Dapr / gRPC / REST** — event and command propagation  
- **Ollama / Local LLMs** — reasoning and embedding models  
- **VS Code / Edge UI** — user interaction layer

Each service runs locally, optionally containerised under Podman or Docker.

---

## 🔮 Future Extensions

| Direction | Concept | Potential Name |
|------------|----------|----------------|
| Shared cloud sync | Unified knowledge vault | **Aerie** |
| Multi-user / group mode | Shared knowledge spaces | **Aviary** |
| Lightweight mobile client | Pocket recall app | **Chick** |
| Long-term personal model | Contextual intelligence layer | **Mentor** |

---

## 💬 Closing Thought

> “Each of us is many minds in conversation.  
>  The Owlet Constellation is those minds, made visible.”  

🦉 **Owlet** knows.  
💡 **Lumen** understands.  
🦢 **Cygnet** remembers.  
🦅 **Eaglet** sees.  
🖤 **Raven** builds.
