#include "DataTypes.h"
#include "CMasterTimer.h"
#include "Setup.h"
#include "Arduino.h"
#include "CUSB.h"
#include "CHead.h"
#include "Config.h"
#include "HWforState.h"

static constexpr uint32_t CHANNELS_BYTESIZE = NUM_CHANNELS * sizeof(int);


DataType::DataType() 
  : state(UNSET), stateTime(0.0), hardwareState(0), sensorState(0) { 
  timestamp = Timer.getConnectTime();
  stateTime = Timer.getStateTime();

  memset(&channels[0], 0, CHANNELS_BYTESIZE );
}

DataType::DataType(StateType state) 
  : state(state), stateTime(0.0), hardwareState(0), sensorState(0) {

  timestamp = Timer.getConnectTime();
  stateTime = Timer.getStateTime();

  memset(&channels[0], 0, CHANNELS_BYTESIZE ); 
}

void DataType::debugSerial() {
  USB.printf("C0:%d\n", channels[0]);
}

void DataType::writeSerial(bool includeFrameMarkers) {
  if (includeFrameMarkers) USB.write(FRAME_START);
  
  USB.write(state);
  USB.write(timestamp);
  USB.write(stateTime);
  USB.write(hardwareState);
  USB.write(sensorState);
  USB.write((uint8_t*)&channels[0], CHANNELS_BYTESIZE);

  if (includeFrameMarkers) USB.write(FRAME_END);
}

void DataType::fillFromHardware(struct HWforState& HW) {
  static uint8_t seq = 0;
  state = HW.state;

  timestamp = Timer.getConnectTime();
  stateTime = Timer.getStateTime();

  hardwareState =
    (uint64_t(HW.Stage1.mid.getLevel() & 0xFFu) << 56) |
    (uint64_t(HW.Stage1.top.getLevel() & 0xFFu) << 48) |
    (uint64_t(HW.Stage1.bot.getLevel() & 0xFFu) << 40) |
    (uint64_t(++seq)                         << 32) |
    (uint64_t(HW.OpAmp.offsetPot.getLevel() & 0xFFu) << 24) |
    (uint64_t(HW.OpAmp.gainPot  .getLevel() & 0xFFu) << 16) | 
    (0xFFFFu);

  sensorState = (uint32_t(HW.Stage1.lastSensorValue()) << 16) | uint32_t(HW.OpAmp.lastSensorValue());
  memset(&channels[0], 0, CHANNELS_BYTESIZE);
}



BlockType::BlockType() : timestamp(0.0), state(UNSET), count(0), data(), numEvents(0), events() {}

void BlockType::clear() {
  timestamp = 0.0;
  state = UNSET;
  count = 0;
  numEvents = 0;
}

bool BlockType::tryAddEvent(const EventKind kind, double stateTime) {
  if (stateTime < 0) stateTime = Timer.getStateTime();

  if (numEvents >= CFG::MAX_EVENTS_PER_BLOCK) return false;

  EventType& event = events[numEvents++];
  event.kind = kind;
  event.stateTime = stateTime;

  return true;
}

void BlockType::writeSerial(bool includeFrameMarkers) {
  if (includeFrameMarkers) USB.write(FRAME_START);

  USB.write(state);
  USB.write(timestamp);
  USB.write(count);
  USB.write(numEvents);

  for (uint32_t i = 0; i < count; i++) {
    DataType& item = data[i];

    // omit state as all data in block share same state
    USB.write(item.timestamp);
    USB.write(item.stateTime);
    USB.write(item.hardwareState);
    USB.write(item.sensorState);
    USB.write((uint8_t*)&item.channels[0], CHANNELS_BYTESIZE);
  }

  for (uint32_t i = 0; i < numEvents; i++) {
    EventType& event = events[i];
    USB.write((uint8_t&)event.kind);
    USB.write(event.stateTime);
  }
  
  if (includeFrameMarkers) USB.write(FRAME_END);
}

void BlockType::debugSerial() {
  USB.printf("N:%d", count);
  uint32_t limit = std::min(count, DEBUG_BLOCKSIZE);
  for(uint32_t i = 0; i < limit; i++) {
    USB.printf("\t C%d:%d", i, data[i].channels[0]);
  }
  USB.printf("\n");
}

