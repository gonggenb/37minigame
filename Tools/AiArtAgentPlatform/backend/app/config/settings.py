from pathlib import Path
from typing import Literal

from pydantic import Field, SecretStr, field_validator
from pydantic_settings import BaseSettings, SettingsConfigDict

PLATFORM_ROOT = Path(__file__).resolve().parents[3]


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=PLATFORM_ROOT / ".env",
        env_prefix="AI_ART_",
        extra="ignore",
        populate_by_name=True,
    )

    host: Literal["127.0.0.1", "localhost"] = "127.0.0.1"
    port: int = Field(default=8765, ge=1, le=65535)
    data_dir: Path = PLATFORM_ROOT / "data"
    preset_dir: Path = PLATFORM_ROOT / "shared" / "presets"
    openai_api_key: SecretStr | None = Field(
        default=None,
        validation_alias="OPENAI_API_KEY",
    )
    openai_review_model: str = Field(
        default="gpt-5.6",
        validation_alias="OPENAI_REVIEW_MODEL",
    )
    openai_image_model: str = Field(
        default="gpt-image-2",
        validation_alias="OPENAI_IMAGE_MODEL",
    )
    openai_timeout_seconds: float = Field(
        default=120,
        ge=1,
        le=300,
        validation_alias="OPENAI_TIMEOUT_SECONDS",
    )
    openai_max_retries: int = Field(
        default=2,
        ge=0,
        le=5,
        validation_alias="OPENAI_MAX_RETRIES",
    )

    @field_validator("openai_api_key", mode="before")
    @classmethod
    def normalize_empty_api_key(cls, value: object) -> object:
        if value == "":
            return None
        return value

    @field_validator("data_dir", "preset_dir")
    @classmethod
    def resolve_platform_path(cls, value: Path) -> Path:
        path = value if value.is_absolute() else PLATFORM_ROOT / value
        return path.resolve()

    def public_status(self) -> dict[str, str | int | float | bool]:
        return {
            "host": self.host,
            "port": self.port,
            "data_dir": str(self.data_dir),
            "api_key_configured": self.openai_api_key is not None,
            "review_model": self.openai_review_model,
            "image_model": self.openai_image_model,
            "timeout_seconds": self.openai_timeout_seconds,
            "max_retries": self.openai_max_retries,
        }
