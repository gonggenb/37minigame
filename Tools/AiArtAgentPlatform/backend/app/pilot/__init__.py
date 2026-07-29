"""真实素材的只读离线试点。"""

from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from .runner import OfflinePilotRunner

__all__ = ["OfflinePilotRunner"]


def __getattr__(name: str) -> object:
    if name == "OfflinePilotRunner":
        from .runner import OfflinePilotRunner

        return OfflinePilotRunner
    raise AttributeError(name)
