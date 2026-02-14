dashboard:
	uv run tensorboard --logdir results --port 6006 --bind_all

CONFIG = Assets/DodgingAgent/config/drone_beefy.yaml
NUM_ENVS ?= 64
NUM_AREAS ?= 16
ARGS ?=

PROJECT_ROOT = /Users/marcocassar/Projects/UnityMLagents/UnityMLPlayground

.PHONY: custom_train
custom_train:
	PYTHONPATH=$(PROJECT_ROOT) uv run mlagents-learn $(CONFIG) \
		--env=builds/$(MODEL).x86_64 \
		--run-id=$(RUN) \
		--num-envs=1 \
		--num-areas=1 \
		$(ARGS)

.PHONY: train
train:
	uv run mlagents-learn $(CONFIG) \
		--env=builds/$(MODEL).x86_64 \
		--run-id=$(RUN) \
		--num-envs=$(NUM_ENVS) \
		--num-areas=$(NUM_AREAS) \
		--no-graphics \
		$(ARGS)
