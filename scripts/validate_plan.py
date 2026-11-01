#!/usr/bin/env python3
"""Offline planning validator. No application execution or Windows network APIs."""
from pathlib import Path
import json
import re
import sys
from urllib.parse import unquote

STATES = {"NOT_STARTED", "IN_PROGRESS", "AWAITING_REVIEW", "EVIDENCE_RECORDED", "COMPLETED",
          "AUDIT_RECORDED_AWAITING_REVIEW", "PHASE_02_IN_PROGRESS", "PHASE_02_AWAITING_REVIEW"}
ORIGINAL_COUNTS = [5, 5, 5, 5, 4, 4, 4, 5, 5]


def check_link(root, path, link):
    if link.startswith(("http://", "https://", "#", "mailto:")):
        return
    # GitHub interprets this README link relative to its repository route.
    if path == root / "README.md" and link == "../../actions/workflows/build.yml":
        assert (root / ".github/workflows/build.yml").is_file(), "missing linked workflow"
        return
    if path == root / "README.md" and link in {"../../releases", "../../discussions"}:
        return  # Known GitHub repository route, not a local document.
    if path == root / "SECURITY.md" and link == "../../security/advisories/new":
        return
    target = (path.parent / unquote(link.split("#", 1)[0])).resolve()
    assert target.exists(), f"broken link: {path.relative_to(root)} -> {link}"


def validate(root):
    plan = json.loads((root / "machine/plan.json").read_text(encoding="utf-8"))
    backlog = (root / "TASKS.md").read_text(encoding="utf-8")
    for name in ["AGENTS.md", "PROJECT_CONTEXT.md", "ROADMAP.md", "STATUS.md", "TASKS.md",
                 "prompts/CODEX_BOOTSTRAP.md", "prompts/CODEX_RESUME.md", "templates/SESSION_HANDOFF.md",
                 "RELEASE_CHECKLIST.md", "docs/security/THREAT_MODEL.md", "docs/testing/TEST_MATRIX.md"]:
        assert (root / name).is_file(), f"missing {name}"
    assert plan["status"] in STATES, "invalid plan status"
    assert plan["activePhase"] in {f"{i:02d}" for i in range(1, 10)}, "invalid active phase"
    assert type(plan["phase02Authorized"]) is bool
    if plan["activePhase"] == "02":
        assert plan["phase02Authorized"] is True, "Phase 02 lacks authorization"
    assert len(plan["phases"]) == 9
    all_ids = []
    for number, phase in enumerate(plan["phases"], 1):
        assert phase["id"] == f"{number:02d}"
        assert phase["status"] in STATES, "invalid phase status"
        text = (root / phase["file"]).read_text(encoding="utf-8")
        section = re.search(rf"## Phase {number:02d}:.*?(?=\n## Phase |\Z)", backlog, re.S)
        assert section, f"missing backlog phase {number}"
        tasks = re.findall(r"- \[([ x])\] \*\*([^*]+)\*\* — (.*?)\. \*\*AC:\*\* (.*?)(?:\n|$)", section.group())
        phase_tasks = re.findall(r"- \[([ x])\] \*\*(\d{2}-T\d{2})\*\*", text)
        assert len(tasks) == len(phase["tasks"]) == len(phase_tasks), "task count mismatch"
        for task, (checked, task_id, title, ac), phase_check in zip(phase["tasks"], tasks, phase_tasks):
            assert (task["id"], task["title"], task["acceptance"]) == (task_id, title, ac.rstrip(".")), task_id
            assert phase_check == (checked, task_id), f"phase checkbox mismatch {task_id}"
            assert f"**{task_id}** — {title}." in text and f"**Acceptance:** {ac}" in text, task_id
            assert task["status"] in STATES, f"invalid task status {task_id}"
            assert (checked == "x") == (task["status"] == "COMPLETED"), f"checkbox/state mismatch {task_id}"
            all_ids.append(task_id)
        assert {f"{number:02d}-T{i:02d}" for i in range(1, ORIGINAL_COUNTS[number-1]+1)} <= {t["id"] for t in phase["tasks"]}, "stable IDs removed"
    assert len(all_ids) == len(set(all_ids)) == 49, "duplicate/missing stable task IDs"
    assert set(plan["preparationCheckpoint"]) <= set(all_ids)
    schema = json.loads((root / "schemas/project.avnet.schema.json").read_text(encoding="utf-8"))
    example = json.loads((root / "examples/project.avnet.json").read_text(encoding="utf-8"))
    from jsonschema import Draft202012Validator, FormatChecker
    Draft202012Validator.check_schema(schema)
    Draft202012Validator(schema, format_checker=FormatChecker()).validate(example)
    ids = [p["id"] for p in example["profiles"]]
    assert len(ids) == len(set(ids))
    assert all(d.get("profileId") is None or d["profileId"] in ids for d in example["devices"])
    links = 0
    # Limit to documentation/source roots; do not traverse build output or user projects.
    files = list(root.glob("*.md"))
    for folder in ["decisions", "docs", "phases", "prompts", "templates"]:
        files.extend((root / folder).rglob("*.md"))
    for path in files:
        contents = re.sub(r"```.*?```", "", path.read_text(encoding="utf-8"), flags=re.S)
        for link in re.findall(r"\]\(([^)]+)\)", contents):
            check_link(root, path, link)
            links += 1
    print(f"PASS: 9 phases, {len(all_ids)} stable tasks, states/checkboxes, draft-2020-12 + formats, references and {links} links; no OS actions")


if __name__ == "__main__":
    validate(Path(__file__).resolve().parents[1])
