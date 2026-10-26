import { createCanvas, CanvasError, joinSession } from "@github/copilot-sdk/extension";
import { createPreviewInstance } from "./server.mjs";

const instances = new Map();

const getInstanceId = (context) => {
  if (
    !context ||
    typeof context.instanceId !== "string" ||
    context.instanceId.length === 0 ||
    context.instanceId.length > 200
  ) {
    throw new CanvasError("invalid-instance", "A valid canvas instance ID is required.");
  }

  return context.instanceId;
};

const getInstance = (instanceId) => {
  const instance = instances.get(instanceId);
  if (!instance) {
    throw new CanvasError("instance-not-open", "Open the demo canvas before using its actions.");
  }

  return instance;
};

export default createCanvas({
  id: "halloween-2026-feature-preview",
  title: "Feature Preview",
  description: "A small interactive demo for previewing canvas behavior.",
  actions: [
    {
      name: "increment-demo-counter",
      description: "Increment the demo counter and return its current value.",
      handler: async (context) => {
        const instance = getInstance(getInstanceId(context));
        return instance.increment();
      },
    },
    {
      name: "reset-demo-counter",
      description: "Reset the demo counter to zero and return its current value.",
      handler: async (context) => {
        const instance = getInstance(getInstanceId(context));
        return instance.reset();
      },
    },
  ],
  open: async (context) => {
    const instanceId = getInstanceId(context);
    let session;
    try {
      session = await joinSession();
      let instance = instances.get(instanceId);
      if (!instance || !instance.isRunning()) {
        instance = await createPreviewInstance();
        instances.set(instanceId, instance);
      }

      session.log(`Opened feature preview canvas instance ${instanceId}.`);
      return instance.url;
    } catch (error) {
      if (error instanceof CanvasError) {
        throw error;
      }

      session?.log(`Failed to open feature preview canvas: ${error.message}`);
      throw new CanvasError("preview-open-failed", "The feature preview could not be opened.");
    }
  },
  onClose: async (context) => {
    const instanceId = getInstanceId(context);
    const instance = instances.get(instanceId);
    if (instance) {
      await instance.close();
      instances.delete(instanceId);
    }
  },
});
