"""Regression tests for planning state, identity, schema formats and link checks."""
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from validate_plan import validate, check_link

ROOT = Path(__file__).resolve().parents[1]


class PlanValidatorTests(unittest.TestCase):
    def test_current_repository(self):
        validate(ROOT)

    def test_owner_retired_entry_point_is_not_required(self):
        original = Path.is_file

        def exists(path):
            return False if path == ROOT / "CODEX_START.md" else original(path)

        with patch.object(Path, "is_file", exists):
            validate(ROOT)

    def test_bootstrap_and_handoff_documents_remain_required(self):
        original = Path.is_file
        for name in ["AGENTS.md", "PROJECT_CONTEXT.md", "ROADMAP.md", "STATUS.md", "TASKS.md",
                     "prompts/CODEX_BOOTSTRAP.md", "prompts/CODEX_RESUME.md", "templates/SESSION_HANDOFF.md"]:
            with self.subTest(document=name):
                def exists(path):
                    return False if path == ROOT / name else original(path)

                with patch.object(Path, "is_file", exists), self.assertRaisesRegex(AssertionError, f"missing {name}"):
                    validate(ROOT)

    def reject_plan_mutation(self, mutate):
        original = Path.read_text
        plan = json.loads((ROOT / "machine/plan.json").read_text(encoding="utf-8"))
        mutate(plan)

        def read(path, *args, **kwargs):
            if path == ROOT / "machine/plan.json":
                return json.dumps(plan)
            return original(path, *args, **kwargs)

        with patch.object(Path, "read_text", read), self.assertRaises(AssertionError):
            validate(ROOT)

    def test_invalid_state(self):
        self.reject_plan_mutation(lambda p: p.update(status="GREEN_ENOUGH"))

    def test_missing_authorization(self):
        self.reject_plan_mutation(lambda p: p.update(activePhase="02", phase02Authorized=False))

    def test_removed_task(self):
        self.reject_plan_mutation(lambda p: p["phases"][0]["tasks"].pop())

    def test_wrong_title(self):
        self.reject_plan_mutation(lambda p: p["phases"][0]["tasks"][0].update(title="different"))

    def test_false_completion(self):
        self.reject_plan_mutation(lambda p: p["phases"][0]["tasks"][0].update(status="COMPLETED"))

    def test_github_relative_workflow_is_checked(self):
        check_link(ROOT, ROOT / "README.md", "../../actions/workflows/build.yml")
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with self.assertRaises(AssertionError):
                check_link(root, root / "README.md", "../../actions/workflows/build.yml")

    def test_other_broken_links_are_not_ignored(self):
        for path, link in [(ROOT / "README.md", "../../actions/workflows/missing.yml"),
                           (ROOT / "docs/spec/PROJECT_JSON_SPEC.md", "../../missing.md")]:
            with self.assertRaises(AssertionError):
                check_link(ROOT, path, link)

    def test_known_github_routes(self):
        for link in ["../../releases", "../../discussions"]:
            check_link(ROOT, ROOT / "README.md", link)
            with self.assertRaises(AssertionError):
                check_link(ROOT, ROOT / "docs/spec/PROJECT_JSON_SPEC.md", link)

    def test_schema_formats_are_assertions(self):
        from jsonschema import Draft202012Validator, FormatChecker, ValidationError
        schema = json.loads((ROOT / "schemas/project.avnet.schema.json").read_text(encoding="utf-8"))
        data = json.loads((ROOT / "examples/project.avnet.json").read_text(encoding="utf-8"))
        data["profiles"][0]["ipv4"]["address"] = "999.0.0.1"
        with self.assertRaises(ValidationError):
            Draft202012Validator(schema, format_checker=FormatChecker()).validate(data)


if __name__ == "__main__":
    unittest.main()
