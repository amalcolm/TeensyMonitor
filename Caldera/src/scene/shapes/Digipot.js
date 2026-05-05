import { COMPONENT_BLUE, COMPONENT_STROKE_WIDTH, makeFilledPolygon, makeLineLoop } from "../drawing.js";
import { isKnownVoltage } from "../voltage.js";
import { Shape } from "./Shape.js";
import { TextLabel } from "./TextLabel.js";

export class Digipot extends Shape {
  constructor({ color = COMPONENT_BLUE, label = "", position = [0, 0, 0], value = 128 } = {}) {
    super({ name: "Digipot", position });

    const bodyLeft = -0.48;
    const bodyRight = 0.08;
    const bodyTop = 0.95;
    const bodyBottom = -0.95;
    const topInputY = 0.62;
    const bottomInputY = -0.62;
    const wiperLeftX = bodyLeft - 0.06;
    const wiperShoulderX = bodyRight + 0.05;
    const wiperTipX = wiperShoulderX + 0.15;
    const wiperHalfHeight = 0.09;

    this.bodyTop = bodyTop;
    this.bodyBottom = bodyBottom;
    this.wiperLeftX = wiperLeftX;
    this.wiperLabelX = (wiperLeftX + wiperShoulderX) / 2;
    this.wiperTipX = wiperTipX;
    this.wiperHalfHeight = wiperHalfHeight;
    this.wiperY = 0;
    this.value = 0;

    this.topInputPort = this.addPort("topInput", [bodyLeft, topInputY], { kind: "input" });
    this.bottomInputPort = this.addPort("bottomInput", [bodyLeft, bottomInputY], { kind: "input" });
    this.wiperPort = this.addPort("wiper", [wiperTipX, this.wiperY], { kind: "output" });

    this.add(makeLineLoop([
      [bodyLeft, bodyTop],
      [bodyRight, bodyTop],
      [bodyRight, bodyBottom],
      [bodyLeft, bodyBottom],
    ], { color, width: COMPONENT_STROKE_WIDTH }));

    this.wiper = makeFilledPolygon([
      [wiperLeftX, wiperHalfHeight],
      [wiperShoulderX, wiperHalfHeight],
      [wiperTipX, 0],
      [wiperShoulderX, -wiperHalfHeight],
      [wiperLeftX, -wiperHalfHeight],
    ], { color });
    this.add(this.wiper);

    if (label) {
      this.add(new TextLabel(label, {
        color,
        height: 0.3,
        position: [(bodyLeft + bodyRight) / 2, bodyTop - 0.12, 0],
        width: 0.38,
      }));
    }

    this.wiperValueLabel = new TextLabel("128", {
      color: "#ffffff",
      height: 0.2,
      position: [this.wiperLabelX, 0, 0.01],
      renderOrder: 3,
      width: 0.28,
    });
    this.wiper.add(this.wiperValueLabel);
    this.setWiperValue(value);
  }

  containsWiperPoint(worldPoint) {
    const localPoint = this.worldToLocal(worldPoint.clone());
    const padding = 0.08;

    return (
      localPoint.x >= this.wiperLeftX - padding
      && localPoint.x <= this.wiperTipX + padding
      && localPoint.y >= this.wiperY - this.wiperHalfHeight - padding
      && localPoint.y <= this.wiperY + this.wiperHalfHeight + padding
    );
  }

  getWiperDragOffset(worldPoint) {
    const localPoint = this.worldToLocal(worldPoint.clone());
    return this.wiperY - localPoint.y;
  }

  dragWiperTo(worldPoint, offsetY = 0) {
    const localPoint = this.worldToLocal(worldPoint.clone());
    this.setWiperY(localPoint.y + offsetY);
  }

  setWiperY(y) {
    this.wiperY = clamp(y, this.bodyBottom, this.bodyTop);
    this.value = this.getValueForY(this.wiperY);
    this.updateWiper();
  }

  setWiperValue(value) {
    this.value = clamp(Math.round(value), 0, 255);
    this.wiperY = this.getYForValue(this.value);
    this.updateWiper();
  }

  snapWiper() {
    this.setWiperValue(this.getValueForY(this.wiperY));
  }

  updateWiper() {
    this.wiper.position.y = this.wiperY;
    this.wiperPort.position.y = this.wiperY;
    this.wiperValueLabel.setText(String(this.value));
  }

  getValueForY(y) {
    const travel = this.bodyTop - this.bodyBottom;
    const normalised = (clamp(y, this.bodyBottom, this.bodyTop) - this.bodyBottom) / travel;

    return clamp(Math.round(normalised * 255), 0, 255);
  }

  getYForValue(value) {
    const normalised = clamp(Math.round(value), 0, 255) / 255;

    return this.bodyBottom + normalised * (this.bodyTop - this.bodyBottom);
  }

  evaluateVoltage() {
    const topVoltage = this.topInputPort.voltage;
    const bottomVoltage = this.bottomInputPort.voltage;

    if (!isKnownVoltage(topVoltage) || !isKnownVoltage(bottomVoltage)) {
      this.wiperPort.voltage = null;
      return;
    }

    const travel = this.value / 255;
    this.wiperPort.voltage = bottomVoltage + (topVoltage - bottomVoltage) * travel;
  }
}

function clamp(value, min, max) {
  return Math.min(Math.max(value, min), max);
}
