from pathlib import Path

import pytest
from app.config.settings import Settings
from pydantic import ValidationError


def test_settings_bind_only_to_loopback(tmp_path: Path) -> None:
    settings = Settings(data_dir=tmp_path)

    assert settings.host == "127.0.0.1"
    assert settings.port == 8765


def test_settings_reject_non_loopback_host(tmp_path: Path) -> None:
    with pytest.raises(ValidationError):
        Settings(host="0.0.0.0", data_dir=tmp_path)


def test_api_key_is_not_serialized(tmp_path: Path) -> None:
    settings = Settings(data_dir=tmp_path, openai_api_key="secret")
    public_status = settings.public_status()

    assert "secret" not in str(public_status)
    assert "openai_api_key" not in public_status
    assert public_status["api_key_configured"] is True
